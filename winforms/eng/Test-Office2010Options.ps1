param(
    [Parameter(Mandatory)][string]$BaselinePackageVersion,
    [string]$BaselineEvidence,
    [string]$Destination,
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug'
)
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path $PSScriptRoot -Parent
$state = Get-Content -LiteralPath (Join-Path $nativeRoot 'samples/RibbonKit.WinForms.DesignerSample/.local/last-refresh.json') -Raw | ConvertFrom-Json
if (-not $Destination) { $Destination = Join-Path $nativeRoot ('artifacts/office2010-f6/public-' + [DateTime]::UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'")) }
$Destination = [IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $Destination) { throw "Preserve existing evidence: $Destination" }
if ($BaselineEvidence) { $BaselineEvidence=[IO.Path]::GetFullPath($BaselineEvidence); if(-not (Test-Path -LiteralPath (Join-Path $BaselineEvidence 'measurements.json'))) { throw 'Baseline measurements missing.' } }
foreach ($scope in @('baseline','current')) {
    if ($scope -eq 'baseline' -and $BaselineEvidence) { continue }
    $root = Join-Path $Destination $scope; $app = Join-Path $root 'App'; New-Item -ItemType Directory -Path $app -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $nativeRoot 'tests/RibbonKit.WinForms.Consumer/Office2010OptionsChecks.cs'), (Join-Path $nativeRoot 'tests/RibbonKit.WinForms.Consumer/app.manifest') -Destination $app
    Set-Content -LiteralPath (Join-Path $app 'Directory.Build.props') -Value '<Project />'; Set-Content -LiteralPath (Join-Path $app 'Directory.Build.targets') -Value '<Project />'
    Set-Content -LiteralPath (Join-Path $app 'Program.cs') -Value 'internal static class Program { [System.STAThread] private static int Main(string[] args) => IndependentConsumer.Office2010OptionsChecks.Run(args[0], args[1] == "current"); }'
    $project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>WinExe</OutputType><TargetFramework>net10.0-windows</TargetFramework><UseWindowsForms>true</UseWindowsForms><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><ApplicationManifest>app.manifest</ApplicationManifest><DefineConstants /></PropertyGroup><ItemGroup><PackageReference Include="RibbonKit.WinForms" Version="" /></ItemGroup></Project>'
    $project.Project.ItemGroup.PackageReference.Version = if ($scope -eq 'baseline') { $BaselinePackageVersion } else { $state.PackageVersion }
    if ($scope -eq 'current') { $project.Project.PropertyGroup.DefineConstants = 'RKWF_F6' }
    $path = Join-Path $app 'OptionsConsumer.csproj'; $project.Save($path)
    $config = [xml]'<configuration><packageSources><clear/><add key="local" value=""/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><config><add key="globalPackagesFolder" value="../packages"/></config></configuration>'
    $config.configuration.packageSources.add[0].value = $state.PackageFeed; $configPath = Join-Path $app 'NuGet.Config'; $config.Save($configPath)
    dotnet restore $path --configfile $configPath
    if ($LASTEXITCODE) { throw "$scope restore failed." }
    dotnet build $path -c $Configuration --no-restore
    if ($LASTEXITCODE) { throw "$scope consumer build failed." }
    $output = Join-Path $root 'evidence'
    $p = Start-Process -FilePath (Join-Path $app "bin/$Configuration/net10.0-windows/OptionsConsumer.exe") -ArgumentList ('"' + $output + '" ' + $scope) -WindowStyle Hidden -PassThru
    if (-not $p.WaitForExit(45000)) { Stop-Process -Id $p.Id; throw "$scope consumer timed out." }
    foreach ($file in @('result.log','failure.log')) { $log = Join-Path $output $file; if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log } }
    if ($p.ExitCode) { throw "$scope consumer failed." }
}
if (-not $BaselineEvidence) { $BaselineEvidence=Join-Path $Destination 'baseline/evidence' }
$before = Get-Content -LiteralPath (Join-Path $BaselineEvidence 'measurements.json') -Raw | ConvertFrom-Json
$after = Get-Content -LiteralPath (Join-Path $Destination 'current/evidence/measurements.json') -Raw | ConvertFrom-Json
foreach ($family in @('2013','2019','2024')) { if ($before.LegacyHashes.$family -ne $after.LegacyHashes.$family) { throw "$family ordinary button paint changed." } }
foreach ($accent in @('Blue','Red')) {
    if ($after.States.$accent.SelectedBorderPixels -le 0 -or $after.States.$accent.HoverBorderPixels -le 0) { throw "$accent row outlines missing." }
    if (($before.States.$accent.ButtonSamples.ordinary0 -join ',') -eq ($after.States.$accent.ButtonSamples.ordinary0 -join ',')) { throw "$accent idle button did not change." }
}
@{ BaselineVersion=$BaselinePackageVersion; CurrentVersion=$state.PackageVersion; OtherFamiliesUnchanged=@('2013','2019','2024'); Before=$before.States; After=$after.States } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Destination 'comparison.json')
