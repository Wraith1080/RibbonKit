param(
    [Parameter(Mandatory)][string]$BaselineAssembly,
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug',
    [switch]$ApplicationButton,
    [string]$Destination
)
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path $PSScriptRoot -Parent
$BaselineAssembly = (Resolve-Path -LiteralPath $BaselineAssembly).Path
if (-not $Destination) {
    $sliceDirectory = if ($ApplicationButton) { 'office2007-g3' } else { 'office2007-g2' }
    $Destination = Join-Path $nativeRoot ('artifacts/' + $sliceDirectory + '/consumer-' + [DateTime]::UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'"))
}
$Destination = [IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $Destination) { throw "Preserve existing evidence: $Destination" }
New-Item -ItemType Directory -Path $Destination | Out-Null
$project = Join-Path $nativeRoot 'tests/RibbonKit.WinForms.PaintConsumer/RibbonKit.WinForms.PaintConsumer.csproj'
foreach ($scope in @('baseline','current')) {
    $buildArgs = @('build', $project, '-c', $Configuration, '-m:1', '-v', 'minimal')
    if ($scope -eq 'baseline') { $buildArgs += "-p:PaintBaselineAssembly=$BaselineAssembly" }
    & dotnet @buildArgs | Tee-Object -FilePath (Join-Path $Destination "$scope-build.log")
    if ($LASTEXITCODE) { throw "$scope consumer build failed." }
    $output = Join-Path $Destination $scope
    $app = Join-Path $nativeRoot "tests/RibbonKit.WinForms.PaintConsumer/bin/$Configuration/net10.0-windows/RibbonKit.WinForms.PaintConsumer.exe"
    $arguments = '"' + $output + '"'
    if ($ApplicationButton) { $arguments += ' orb-only' }
    $process = Start-Process -FilePath $app -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    Get-Content -LiteralPath (Join-Path $output 'result.log')
    if ($process.ExitCode) { throw "$scope consumer failed." }
}
$count = 0
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $Destination 'baseline') -Filter 'default-*.png' -File) {
    $current = Join-Path $Destination ('current/' + $file.Name)
    if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $current).Hash) { throw "Default paint changed: $($file.Name)" }
    $count++
}
if ($count -ne 32) { throw "Expected 32 default image pairs, got $count." }
@{ BaselineAssembly = $BaselineAssembly; BaselineHash = (Get-FileHash -LiteralPath $BaselineAssembly).Hash; Configuration = $Configuration; ExactPairs = $count } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Destination 'comparison.json')
"PASS: $count byte-exact default paint pairs; opt-in public paint/native checks pass." | Tee-Object -FilePath (Join-Path $Destination 'result.log')
