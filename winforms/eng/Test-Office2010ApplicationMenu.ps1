param(
    [Parameter(Mandatory)][string]$BaselinePackageVersion,
    [string]$Destination,
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug'
)
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path $PSScriptRoot -Parent
$state = Get-Content -LiteralPath (Join-Path $nativeRoot 'samples/RibbonKit.WinForms.DesignerSample/.local/last-refresh.json') -Raw | ConvertFrom-Json
if (-not $Destination) { $Destination = Join-Path $nativeRoot ('artifacts/office2010-f5/public-' + [DateTime]::UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'")) }
$Destination = [IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $Destination) { throw "Preserve existing evidence: $Destination" }
foreach ($scope in @('baseline','current')) {
    $root = Join-Path $Destination $scope; $app = Join-Path $root 'App'; New-Item -ItemType Directory -Path $app -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $nativeRoot 'tests/RibbonKit.WinForms.Consumer/Office2010ApplicationMenuChecks.cs'), (Join-Path $nativeRoot 'tests/RibbonKit.WinForms.Consumer/app.manifest') -Destination $app
    Set-Content -LiteralPath (Join-Path $app 'Directory.Build.props') -Value '<Project />'; Set-Content -LiteralPath (Join-Path $app 'Directory.Build.targets') -Value '<Project />'
    Set-Content -LiteralPath (Join-Path $app 'Program.cs') -Value 'internal static class Program { [System.STAThread] private static int Main(string[] args) => IndependentConsumer.Office2010ApplicationMenuChecks.Run(args[0], args[1] == "current"); }'
    $project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>WinExe</OutputType><TargetFramework>net10.0-windows</TargetFramework><UseWindowsForms>true</UseWindowsForms><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><ApplicationManifest>app.manifest</ApplicationManifest></PropertyGroup><ItemGroup><PackageReference Include="RibbonKit.WinForms" Version="" /></ItemGroup></Project>'
    $project.Project.ItemGroup.PackageReference.Version = if ($scope -eq 'baseline') { $BaselinePackageVersion } else { $state.PackageVersion }
    $path = Join-Path $app 'MenuConsumer.csproj'; $project.Save($path)
    $config = [xml]'<configuration><packageSources><clear/><add key="local" value=""/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><config><add key="globalPackagesFolder" value="../packages"/></config></configuration>'
    $config.configuration.packageSources.add[0].value = $state.PackageFeed; $configPath = Join-Path $app 'NuGet.Config'; $config.Save($configPath)
    dotnet restore $path --configfile $configPath
    if ($LASTEXITCODE) { throw "$scope restore failed." }
    dotnet build $path -c $Configuration --no-restore
    if ($LASTEXITCODE) { throw "$scope consumer build failed." }
    $output = Join-Path $root 'evidence'
    $p = Start-Process -FilePath (Join-Path $app "bin/$Configuration/net10.0-windows/MenuConsumer.exe") -ArgumentList ('"' + $output + '" ' + $scope) -WindowStyle Hidden -PassThru
    if (-not $p.WaitForExit(45000)) { Stop-Process -Id $p.Id; throw "$scope consumer timed out." }
    foreach ($file in @('result.log','failure.log')) { $log = Join-Path $output $file; if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log } }
    if ($p.ExitCode) { throw "$scope consumer failed." }
}
$before = Get-Content -LiteralPath (Join-Path $Destination 'baseline/evidence/measurements.json') -Raw | ConvertFrom-Json
$after = Get-Content -LiteralPath (Join-Path $Destination 'current/evidence/measurements.json') -Raw | ConvertFrom-Json
if ($before.HeadingMatchesPane -or -not $after.HeadingMatchesPane) { throw 'Default-heading patch was not reproduced and corrected.' }
foreach ($role in @('footer-1','footer-2')) {
    if (($before.FooterSamples.$role -join ',') -ne ($after.FooterSamples.$role -join ',')) { throw "$role interior fill changed." }
}
if (($before.FooterSamples.'footer-0' -join ',') -eq ($after.FooterSamples.'footer-0' -join ',')) { throw 'Idle footer did not change.' }
foreach ($role in @('footer-1','footer-2','split-0','split-1','split-2','split-3')) {
    if ($before.StateBorderPixels.$role -ne 0 -or $after.StateBorderPixels.$role -le 0) { throw "$role outline correction was not reproduced." }
}
foreach ($family in @('2013','2019','2024')) { if ($before.LegacyHashes.$family -ne $after.LegacyHashes.$family) { throw "$family menu paint changed." } }
if (($before.Geometry | ConvertTo-Json -Depth 6 -Compress) -ne ($after.Geometry | ConvertTo-Json -Depth 6 -Compress)) { throw 'Menu geometry changed.' }
@{ BaselineVersion = $BaselinePackageVersion; CurrentVersion = $state.PackageVersion; DefaultHeadingCorrected = $true; FooterHoverPressedInteriorsUnchanged = $true; BeforeBorders = $before.StateBorderPixels; AfterBorders = $after.StateBorderPixels; OtherFamiliesUnchanged = @('2013','2019','2024'); GeometryUnchanged = $true } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $Destination 'comparison.json')
