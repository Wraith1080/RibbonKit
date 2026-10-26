param([string]$OutputDirectory, [ValidateSet('All', 'Startup', 'Popups')][string]$Scope = 'All')
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path $PSScriptRoot
if (!$OutputDirectory) { $OutputDirectory = Join-Path $nativeRoot 'TestResults/baseline' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
Push-Location $nativeRoot
try {
    dotnet build RibbonKit.WinForms.sln -c Release
    if ($LASTEXITCODE) { throw 'Release build failed.' }
    $sampleExe = Join-Path $nativeRoot 'samples/RibbonKit.WinForms.Showcase/bin/Release/net10.0-windows/RibbonKit.WinForms.Showcase.exe'
    if ($Scope -ne 'Popups') {
      $startup = for ($i = 0; $i -lt 10; $i++) {
        $startInfo = [Diagnostics.ProcessStartInfo]::new($sampleExe, '--measure-startup')
        $startInfo.UseShellExecute = $false; $startInfo.RedirectStandardOutput = $true
        $process = [Diagnostics.Process]::Start($startInfo)
        try {
            if (!$process.WaitForExit(15000)) { $process.Kill(); throw 'Startup measurement timed out.' }
            if ($process.ExitCode) { throw 'Startup measurement process failed.' }
            $row = $process.StandardOutput.ReadToEnd() | ConvertFrom-Json
            [pscustomobject]@{ Launch = $i + 1; ManagedReadyMs = $row.ManagedReadyMs; ProcessAgeMs = $row.ProcessAgeMs }
        } finally { $process.Dispose() }
      }
      $startup | ConvertTo-Json | Set-Content (Join-Path $OutputDirectory 'startup.json')
    }
    if ($Scope -ne 'Startup') {
      $benchmarkMode = if ($Scope -eq 'Popups') { '--benchmark-popups' } else { '--benchmark' }
      $benchmarkFile = if ($Scope -eq 'Popups') { 'native-popups.json' } else { 'native-baseline.json' }
      dotnet run --project tests/RibbonKit.WinForms.NativeTests -c Release --no-build -- $benchmarkMode (Join-Path $OutputDirectory $benchmarkFile)
      if ($LASTEXITCODE) { throw 'Native baseline workload failed.' }
    }
    Write-Host "Raw baseline retained at $OutputDirectory"
} finally { Pop-Location }
