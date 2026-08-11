[CmdletBinding()]
param(
    [string]$ExecutablePath = (Join-Path $PSScriptRoot '..\samples\RibbonKit.Showcase\bin\Release\net8.0-windows\RibbonKit.Showcase.exe'),
    [ValidateRange(1, 20)]
    [int]$StartupRuns = 5,
    [ValidateRange(10, 1000)]
    [int]$ResizeIterations = 160,
    [ValidateRange(1, 10)]
    [int]$MeasuredResizePasses = 3,
    [ValidateRange(1, 100)]
    [int]$ResizeIntervalMilliseconds = 8,
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\TestResults\performance\showcase-release.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class RibbonKitPerformanceNativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MoveWindow(
        IntPtr hWnd,
        int x,
        int y,
        int width,
        int height,
        [MarshalAs(UnmanagedType.Bool)] bool repaint);
}
'@

$executablePathValue = [System.IO.Path]::GetFullPath($ExecutablePath)
$outputPathValue = [System.IO.Path]::GetFullPath($OutputPath)

if (-not (Test-Path -LiteralPath $executablePathValue -PathType Leaf)) {
    throw "Showcase executable '$executablePathValue' does not exist. Build it in Release first."
}

function Stop-ShowcaseProcess {
    param([System.Diagnostics.Process]$Process)

    try {
        if (-not $Process.HasExited) {
            $null = $Process.CloseMainWindow()
            if (-not $Process.WaitForExit(5000)) {
                $Process.Kill()
                $null = $Process.WaitForExit(5000)
            }
        }
    }
    finally {
        $Process.Dispose()
    }
}

function Start-ReadyShowcase {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $process = Start-Process -FilePath $executablePathValue -PassThru

    try {
        if (-not $process.WaitForInputIdle(15000)) {
            throw 'Showcase did not reach an input-idle state within 15 seconds.'
        }

        $handleDeadline = [DateTime]::UtcNow.AddSeconds(5)
        while ($process.MainWindowHandle -eq [IntPtr]::Zero -and [DateTime]::UtcNow -lt $handleDeadline) {
            Start-Sleep -Milliseconds 10
            $process.Refresh()
        }

        if ($process.MainWindowHandle -eq [IntPtr]::Zero) {
            throw 'Showcase reached input-idle but did not expose a main window handle.'
        }

        $stopwatch.Stop()
        return [pscustomobject]@{
            Process = $process
            StartupMilliseconds = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2)
        }
    }
    catch {
        Stop-ShowcaseProcess $process
        throw
    }
}

function Invoke-ResizePass {
    param(
        [System.Diagnostics.Process]$Process,
        $WindowRect,
        [int]$OriginalHeight,
        [int]$MinimumWidth,
        [int]$MaximumWidth
    )

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    for ($iteration = 0; $iteration -lt $ResizeIterations; $iteration++) {
        $phase = ($iteration % 80) / 79.0
        if (([Math]::Floor($iteration / 80) % 2) -eq 1) {
            $phase = 1.0 - $phase
        }
        $width = [int][Math]::Round($MinimumWidth + (($MaximumWidth - $MinimumWidth) * $phase))

        if (-not [RibbonKitPerformanceNativeMethods]::MoveWindow(
            $Process.MainWindowHandle,
            $WindowRect.Left,
            $WindowRect.Top,
            $width,
            $OriginalHeight,
            $true)) {
            throw "MoveWindow failed with Win32 error $([Runtime.InteropServices.Marshal]::GetLastWin32Error())."
        }

        Start-Sleep -Milliseconds $ResizeIntervalMilliseconds
    }
    $stopwatch.Stop()
    return $stopwatch
}

$startupMeasurements = @()
for ($run = 0; $run -lt $StartupRuns; $run++) {
    $measurement = Start-ReadyShowcase
    $startupMeasurements += $measurement.StartupMilliseconds
    Stop-ShowcaseProcess $measurement.Process
}

$orderedStartupMeasurements = @($startupMeasurements | Sort-Object)
$middleIndex = [int][Math]::Floor($orderedStartupMeasurements.Count / 2)
if (($orderedStartupMeasurements.Count % 2) -eq 0) {
    $startupMedian = ($orderedStartupMeasurements[$middleIndex - 1] + $orderedStartupMeasurements[$middleIndex]) / 2
}
else {
    $startupMedian = $orderedStartupMeasurements[$middleIndex]
}

$liveMeasurement = Start-ReadyShowcase
$process = $liveMeasurement.Process

try {
    Start-Sleep -Milliseconds 500
    $process.Refresh()
    $workingSetInitial = $process.WorkingSet64
    $privateMemoryInitial = $process.PrivateMemorySize64

    $windowRect = New-Object RibbonKitPerformanceNativeMethods+RECT
    if (-not [RibbonKitPerformanceNativeMethods]::GetWindowRect($process.MainWindowHandle, [ref]$windowRect)) {
        throw "GetWindowRect failed with Win32 error $([Runtime.InteropServices.Marshal]::GetLastWin32Error())."
    }

    $originalWidth = $windowRect.Right - $windowRect.Left
    $originalHeight = $windowRect.Bottom - $windowRect.Top
    $minimumWidth = [Math]::Min(520, $originalWidth)
    $maximumWidth = [Math]::Max(1280, $originalWidth)

    $warmupStopwatch = Invoke-ResizePass $process $windowRect $originalHeight $minimumWidth $maximumWidth
    $null = [RibbonKitPerformanceNativeMethods]::MoveWindow(
        $process.MainWindowHandle,
        $windowRect.Left,
        $windowRect.Top,
        $originalWidth,
        $originalHeight,
        $true)
    $null = $process.WaitForInputIdle(5000)
    Start-Sleep -Milliseconds 500
    $process.Refresh()

    $workingSetBefore = $process.WorkingSet64
    $privateMemoryBefore = $process.PrivateMemorySize64
    $resizeCpuMilliseconds = 0.0
    $resizeWallMilliseconds = 0.0
    $resizePassResults = @()

    for ($pass = 1; $pass -le $MeasuredResizePasses; $pass++) {
        $process.Refresh()
        $passCpuBefore = $process.TotalProcessorTime.TotalMilliseconds
        $resizeStopwatch = Invoke-ResizePass $process $windowRect $originalHeight $minimumWidth $maximumWidth
        $process.Refresh()
        $passCpuMilliseconds = $process.TotalProcessorTime.TotalMilliseconds - $passCpuBefore

        $null = [RibbonKitPerformanceNativeMethods]::MoveWindow(
            $process.MainWindowHandle,
            $windowRect.Left,
            $windowRect.Top,
            $originalWidth,
            $originalHeight,
            $true)
        $null = $process.WaitForInputIdle(5000)
        Start-Sleep -Milliseconds 500
        $process.Refresh()

        $resizeCpuMilliseconds += $passCpuMilliseconds
        $resizeWallMilliseconds += $resizeStopwatch.Elapsed.TotalMilliseconds
        $resizePassResults += [ordered]@{
            pass = $pass
            wallMilliseconds = [Math]::Round($resizeStopwatch.Elapsed.TotalMilliseconds, 2)
            processCpuMilliseconds = [Math]::Round($passCpuMilliseconds, 2)
            workingSetMiB = [Math]::Round($process.WorkingSet64 / 1MB, 2)
            privateMiB = [Math]::Round($process.PrivateMemorySize64 / 1MB, 2)
        }
    }

    $workingSetAfter = $process.WorkingSet64
    $privateMemoryAfter = $process.PrivateMemorySize64
    $totalMeasuredResizes = $ResizeIterations * $MeasuredResizePasses

    $result = [ordered]@{
        measuredAtUtc = [DateTime]::UtcNow.ToString('o')
        executable = $executablePathValue
        processArchitecture = $process.StartInfo.EnvironmentVariables['PROCESSOR_ARCHITECTURE']
        logicalProcessorCount = [Environment]::ProcessorCount
        startup = [ordered]@{
            definition = 'Process start through input-idle with a nonzero main-window handle'
            runsMilliseconds = $startupMeasurements
            firstMilliseconds = $startupMeasurements[0]
            medianMilliseconds = [Math]::Round($startupMedian, 2)
            minimumMilliseconds = [Math]::Round(($orderedStartupMeasurements | Select-Object -First 1), 2)
            maximumMilliseconds = [Math]::Round(($orderedStartupMeasurements | Select-Object -Last 1), 2)
        }
        resize = [ordered]@{
            warmupIterations = $ResizeIterations
            warmupWallMilliseconds = [Math]::Round($warmupStopwatch.Elapsed.TotalMilliseconds, 2)
            iterationsPerPass = $ResizeIterations
            measuredPasses = $MeasuredResizePasses
            totalMeasuredResizes = $totalMeasuredResizes
            intervalMilliseconds = $ResizeIntervalMilliseconds
            wallMilliseconds = [Math]::Round($resizeWallMilliseconds, 2)
            processCpuMilliseconds = [Math]::Round($resizeCpuMilliseconds, 2)
            processCpuMillisecondsPerResize = [Math]::Round($resizeCpuMilliseconds / $totalMeasuredResizes, 3)
            processCpuPercentOfOneCore = [Math]::Round(($resizeCpuMilliseconds / $resizeWallMilliseconds) * 100, 2)
            passes = $resizePassResults
        }
        memory = [ordered]@{
            workingSetInitialMiB = [Math]::Round($workingSetInitial / 1MB, 2)
            workingSetAfterWarmupMiB = [Math]::Round($workingSetBefore / 1MB, 2)
            workingSetAfterMeasuredPassesMiB = [Math]::Round($workingSetAfter / 1MB, 2)
            workingSetMeasuredPassesChangeMiB = [Math]::Round(($workingSetAfter - $workingSetBefore) / 1MB, 2)
            privateInitialMiB = [Math]::Round($privateMemoryInitial / 1MB, 2)
            privateAfterWarmupMiB = [Math]::Round($privateMemoryBefore / 1MB, 2)
            privateAfterMeasuredPassesMiB = [Math]::Round($privateMemoryAfter / 1MB, 2)
            privateMeasuredPassesChangeMiB = [Math]::Round(($privateMemoryAfter - $privateMemoryBefore) / 1MB, 2)
        }
    }

    $outputDirectory = Split-Path -Parent $outputPathValue
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    $json = $result | ConvertTo-Json -Depth 5
    [System.IO.File]::WriteAllText($outputPathValue, $json, (New-Object System.Text.UTF8Encoding($false)))
    Write-Output $json
}
finally {
    Stop-ShowcaseProcess $process
}
