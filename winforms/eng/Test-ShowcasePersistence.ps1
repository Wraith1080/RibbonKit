param([string]$Artifacts = (Join-Path $PSScriptRoot '../artifacts/showcase-persistence'))
$ErrorActionPreference = 'Stop'
$sampleExe = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../samples/RibbonKit.WinForms.Showcase/bin/Debug/net10.0-windows/RibbonKit.WinForms.Showcase.exe'))
if (!(Test-Path -LiteralPath $sampleExe)) { throw 'Build the Showcase in Debug before running this check.' }
$runRoot = Join-Path ([IO.Path]::GetFullPath($Artifacts)) (Get-Date -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null

function Invoke-Probe([string]$Settings, [string]$Phase, [string]$Name) {
    $result = Join-Path $runRoot ($Name + '.json')
    # All paths are generated locally; quoting is for ProcessStartInfo's Windows argument parser.
    $process = Start-Process -FilePath $sampleExe -ArgumentList @('--persistence-probe', ('"' + $Settings + '"'), $Phase, ('"' + $result + '"')) -PassThru
    if (!$process.WaitForExit(30000)) { $process.Kill(); throw "Probe $Name timed out." }
    if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath $result)) { throw "Probe $Name failed; inspect $result" }
    $report = Get-Content -LiteralPath $result -Raw | ConvertFrom-Json
    if ($report.Result -ne 'PASS') { throw "Probe $Name failed: $(Get-Content -LiteralPath $result -Raw)" }
    Write-Output "PASS: $Name"
}

$settings = Join-Path $runRoot 'settings'
Invoke-Probe $settings 'defaults' 'missing-files'
Invoke-Probe $settings 'write' 'apply-cancel-and-save'
Invoke-Probe $settings 'read' 'fresh-process-restore'
$preferences = Join-Path $settings 'preferences.json'
$profile = Join-Path $settings 'ribbon-customization.json'
Copy-Item -LiteralPath $preferences -Destination (Join-Path $runRoot 'saved-preferences.json')
Copy-Item -LiteralPath $profile -Destination (Join-Path $runRoot 'saved-customization.json')

foreach ($failure in @('malformed', 'future')) {
    if ($failure -eq 'malformed') { $preferenceText = '{broken'; $profileText = '{broken' }
    else {
        $preferenceObject = Get-Content -LiteralPath (Join-Path $runRoot 'saved-preferences.json') -Raw | ConvertFrom-Json
        $preferenceObject.version = 999; $preferenceText = $preferenceObject | ConvertTo-Json -Depth 16
        $profileObject = Get-Content -LiteralPath (Join-Path $runRoot 'saved-customization.json') -Raw | ConvertFrom-Json
        $profileObject.version = 999; $profileText = $profileObject | ConvertTo-Json -Depth 20
    }
    [IO.File]::WriteAllText($preferences, $preferenceText)
    [IO.File]::WriteAllText($profile, $profileText)
    Invoke-Probe $settings 'defaults' $failure
    if ([IO.File]::ReadAllText($preferences) -cne $preferenceText -or [IO.File]::ReadAllText($profile) -cne $profileText) {
        throw "$failure input was overwritten during shutdown."
    }
}
Write-Output "PASS: isolated Showcase persistence and rejected-file preservation. Evidence: $runRoot"
