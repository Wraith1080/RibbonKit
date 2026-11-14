param(
    [string]$BaselinePackageVersion,
    [string]$BaselineEvidence,
    [string]$Destination,
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug'
)
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path $PSScriptRoot -Parent
$state = Get-Content -LiteralPath (Join-Path $nativeRoot 'samples/RibbonKit.WinForms.DesignerSample/.local/last-refresh.json') -Raw | ConvertFrom-Json
if (-not $BaselinePackageVersion -and -not $BaselineEvidence) { throw 'Supply the retained pre-F4 package version or existing baseline evidence.' }
if ($BaselineEvidence) { $BaselineEvidence = [IO.Path]::GetFullPath($BaselineEvidence); if (-not (Test-Path -LiteralPath $BaselineEvidence -PathType Container)) { throw 'Baseline evidence missing.' } }
if (-not $Destination) { $Destination = Join-Path $nativeRoot ('artifacts/office2010-review/public-' + [DateTime]::UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'")) }
$Destination = [IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $Destination) { throw "Preserve existing evidence: $Destination" }
foreach ($scope in @('baseline','current')) {
    if ($scope -eq 'baseline' -and $BaselineEvidence) { continue }
    $root = Join-Path $Destination $scope; $app = Join-Path $root 'App'; New-Item -ItemType Directory -Path $app -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $nativeRoot 'tests/RibbonKit.WinForms.Consumer/Office2010ReviewChecks.cs'), (Join-Path $nativeRoot 'tests/RibbonKit.WinForms.Consumer/app.manifest') -Destination $app
    Set-Content -LiteralPath (Join-Path $app 'Directory.Build.props') -Value '<Project />'; Set-Content -LiteralPath (Join-Path $app 'Directory.Build.targets') -Value '<Project />'
    Set-Content -LiteralPath (Join-Path $app 'Program.cs') -Value 'internal static class Program { [System.STAThread] private static int Main(string[] args) => IndependentConsumer.Office2010ReviewChecks.Run(args[0]); }'
    $project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>WinExe</OutputType><TargetFramework>net10.0-windows</TargetFramework><UseWindowsForms>true</UseWindowsForms><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><ApplicationManifest>app.manifest</ApplicationManifest><DefineConstants /></PropertyGroup><ItemGroup><PackageReference Include="RibbonKit.WinForms" Version="" /></ItemGroup></Project>'
    $project.Project.ItemGroup.PackageReference.Version = if ($scope -eq 'baseline') { $BaselinePackageVersion } else { $state.PackageVersion }
    if ($scope -eq 'current') { $project.Project.PropertyGroup.DefineConstants = 'RKWF_REVIEW' }
    $path = Join-Path $app 'BackstageConsumer.csproj'; $project.Save($path)
    $config = [xml]'<configuration><packageSources><clear/><add key="local" value=""/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><config><add key="globalPackagesFolder" value="../packages"/></config></configuration>'
    $config.configuration.packageSources.add[0].value = $state.PackageFeed; $configPath = Join-Path $app 'NuGet.Config'; $config.Save($configPath)
    dotnet restore $path --configfile $configPath
    if ($LASTEXITCODE) { throw "$scope restore failed." }
    dotnet build $path -c $Configuration --no-restore
    if ($LASTEXITCODE) { throw "$scope consumer build failed." }
    $output = Join-Path $root 'evidence'
    $p = Start-Process -FilePath (Join-Path $app "bin/$Configuration/net10.0-windows/BackstageConsumer.exe") -ArgumentList ('"' + $output + '"') -WindowStyle Hidden -PassThru
    if (-not $p.WaitForExit(45000)) { Stop-Process -Id $p.Id; throw "$scope consumer timed out." }
    foreach ($name in @('result.log','failure.log')) { $log = Join-Path $output $name; if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log } }
    if ($p.ExitCode) { throw "$scope consumer failed." }
}
if (-not $BaselineEvidence) { $BaselineEvidence = Join-Path $Destination 'baseline/evidence' }
$before = Get-Content -LiteralPath (Join-Path $BaselineEvidence 'paint.json') -Raw | ConvertFrom-Json
$after = Get-Content -LiteralPath (Join-Path $Destination 'current/evidence/paint.json') -Raw | ConvertFrom-Json
if ($before.LeakedTextPixels -le 0 -or $after.LeakedTextPixels -ne 0 -or $before.GalleryUtilityBorderPixels -ne 0 -or $after.GalleryUtilityBorderPixels -le 0) { throw 'Expected before/after clipping and utility-outline evidence was not established.' }
@{ BaselineVersion = $BaselinePackageVersion; CurrentVersion = $state.PackageVersion; Before = $before; After = $after } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $Destination 'comparison.json')
Get-Content -LiteralPath (Join-Path $Destination 'comparison.json')
Get-Content -LiteralPath (Join-Path $Destination 'current/evidence/native/result.log')
