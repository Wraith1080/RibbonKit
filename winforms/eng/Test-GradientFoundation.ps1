param(
    [string]$BaselinePackageVersion,
    [string]$BaselineEvidence,
    [string]$Destination,
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug'
)
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path $PSScriptRoot -Parent
$state = Get-Content -LiteralPath (Join-Path $nativeRoot 'samples/RibbonKit.WinForms.DesignerSample/.local/last-refresh.json') -Raw | ConvertFrom-Json
if (-not $BaselinePackageVersion -and -not $BaselineEvidence) { throw 'Supply the pre-F2 package version or retained baseline evidence for an independent default/legacy comparison.' }
if ($BaselineEvidence) { $BaselineEvidence = [IO.Path]::GetFullPath($BaselineEvidence); if (-not (Test-Path -LiteralPath $BaselineEvidence -PathType Container)) { throw 'Baseline evidence directory is missing.' } }
if (-not $Destination) { $Destination = Join-Path $nativeRoot ('artifacts/gradient-foundation/public-' + [DateTime]::UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'")) }
$Destination = [IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $Destination) { throw "Preserve existing evidence: $Destination" }
foreach ($scope in @('baseline','current')) {
    if ($scope -eq 'baseline' -and $BaselineEvidence) { continue }
    $root = Join-Path $Destination $scope; $app = Join-Path $root 'App'; New-Item -ItemType Directory -Path $app -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $nativeRoot 'tests/RibbonKit.WinForms.Consumer/GradientFoundationChecks.cs'), (Join-Path $nativeRoot 'tests/RibbonKit.WinForms.Consumer/app.manifest') -Destination $app
    Set-Content -LiteralPath (Join-Path $app 'Directory.Build.props') -Value '<Project />'
    Set-Content -LiteralPath (Join-Path $app 'Directory.Build.targets') -Value '<Project />'
    Set-Content -LiteralPath (Join-Path $app 'Program.cs') -Value 'internal static class Program { [System.STAThread] private static int Main(string[] args) => IndependentConsumer.GradientFoundationChecks.Run(args[0]); }'
    $project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>WinExe</OutputType><TargetFramework>net10.0-windows</TargetFramework><UseWindowsForms>true</UseWindowsForms><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><ApplicationManifest>app.manifest</ApplicationManifest><DefineConstants /></PropertyGroup><ItemGroup><PackageReference Include="RibbonKit.WinForms" Version="" /></ItemGroup></Project>'
    $project.Project.ItemGroup.PackageReference.Version = if ($scope -eq 'baseline') { $BaselinePackageVersion } else { $state.PackageVersion }
    if ($scope -eq 'current') { $project.Project.PropertyGroup.DefineConstants = 'F2' }
    $projectPath = Join-Path $app 'GradientConsumer.csproj'; $project.Save($projectPath)
    $config = [xml]'<configuration><packageSources><clear/><add key="local" value=""/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><config><add key="globalPackagesFolder" value="../packages"/></config></configuration>'
    $config.configuration.packageSources.add[0].value = $state.PackageFeed
    $configPath = Join-Path $app 'NuGet.Config'; $config.Save($configPath)
    dotnet restore $projectPath --configfile $configPath
    if ($LASTEXITCODE) { throw "$scope package restore failed." }
    dotnet build $projectPath -c $Configuration --no-restore
    if ($LASTEXITCODE) { throw "$scope public consumer build failed." }
    $output = Join-Path $root 'evidence'
    $process = Start-Process -FilePath (Join-Path $app "bin/$Configuration/net10.0-windows/GradientConsumer.exe") -ArgumentList ('"' + $output + '"') -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode) { Get-Content -LiteralPath (Join-Path $output 'result.log'); throw "$scope consumer failed." }
}
$count = 0
if (-not $BaselineEvidence) { $BaselineEvidence = Join-Path $Destination 'baseline/evidence' }
foreach ($file in Get-ChildItem -LiteralPath $BaselineEvidence -Filter '*.png' -File) {
    $current = Join-Path $Destination ('current/evidence/' + $file.Name)
    if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $current).Hash) { throw "Default/legacy paint changed: $($file.Name)" }
    $count++
}
if ($count -ne 24) { throw "Expected 24 default/legacy image pairs, got $count." }
"PASS: $count byte-exact default/legacy image pairs; new public gradient/state/native checks pass." | Tee-Object -FilePath (Join-Path $Destination 'result.log')
@{ BaselineVersion = $BaselinePackageVersion; CurrentVersion = $state.PackageVersion; BaselineEvidence = $BaselineEvidence; ExactPairs = $count } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Destination 'comparison.json')
Get-Content -LiteralPath (Join-Path $Destination 'current/evidence/result.log') | Select-Object -Last 2
