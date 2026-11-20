param(
    [Parameter(Mandatory)][string]$BaselinePackageVersion,
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug',
    [string]$Destination
)
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path $PSScriptRoot -Parent
$state = Get-Content -LiteralPath (Join-Path $nativeRoot 'samples/RibbonKit.WinForms.DesignerSample/.local/last-refresh.json') -Raw | ConvertFrom-Json
if (-not $Destination) { $Destination = Join-Path $nativeRoot ('artifacts/office2007-g5/public-' + [DateTime]::UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'")) }
$Destination = [IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $Destination) { throw "Preserve existing evidence: $Destination" }
foreach ($scope in @('baseline','current')) {
    $app = Join-Path $Destination "$scope/App"; New-Item -ItemType Directory -Path $app -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $nativeRoot 'tests/RibbonKit.WinForms.Consumer/Office2007ApplicationMenuChecks.cs'), (Join-Path $nativeRoot 'tests/RibbonKit.WinForms.Consumer/app.manifest') -Destination $app
    Set-Content -LiteralPath (Join-Path $app 'Directory.Build.props') -Value '<Project />'
    Set-Content -LiteralPath (Join-Path $app 'Directory.Build.targets') -Value '<Project />'
    Set-Content -LiteralPath (Join-Path $app 'Program.cs') -Value 'internal static class Program { [System.STAThread] private static int Main(string[] args) => IndependentConsumer.Office2007ApplicationMenuChecks.Run(args[0], args[1] == "current"); }'
    $project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>WinExe</OutputType><TargetFramework>net10.0-windows</TargetFramework><UseWindowsForms>true</UseWindowsForms><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><ApplicationManifest>app.manifest</ApplicationManifest><DefineConstants /></PropertyGroup><ItemGroup><PackageReference Include="RibbonKit.WinForms" Version="" /></ItemGroup></Project>'
    $project.Project.ItemGroup.PackageReference.Version = if ($scope -eq 'baseline') { $BaselinePackageVersion } else { $state.PackageVersion }
    $project.Project.PropertyGroup.DefineConstants = if ($scope -eq 'baseline') { 'G4_BASELINE' } else { '' }
    $path = Join-Path $app 'MenuConsumer.csproj'; $project.Save($path)
    $config = [xml]'<configuration><packageSources><clear/><add key="local" value=""/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><config><add key="globalPackagesFolder" value=""/></config></configuration>'
    $config.configuration.packageSources.add[0].value = $state.PackageFeed; $config.configuration.config.add.value = $state.PackageCache
    $configPath = Join-Path $app 'NuGet.Config'; $config.Save($configPath)
    dotnet restore $path --configfile $configPath -p:NuGetAudit=false
    if ($LASTEXITCODE) { throw "$scope package restore failed." }
    dotnet build $path -c $Configuration --no-restore -m:1
    if ($LASTEXITCODE) { throw "$scope build failed." }
    $output = Join-Path $Destination "$scope/evidence"
    $process = Start-Process -FilePath (Join-Path $app "bin/$Configuration/net10.0-windows/MenuConsumer.exe") -ArgumentList ('"' + $output + '" ' + $scope) -WindowStyle Hidden -PassThru -RedirectStandardError (Join-Path $Destination "$scope/stderr.log")
    if (-not $process.WaitForExit(45000)) { Stop-Process -Id $process.Id; throw "$scope timed out." }
    foreach ($name in @('result.log','failure.log')) { $log = Join-Path $output $name; if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log } }
    if ($process.ExitCode) { throw "$scope consumer failed: $($process.ExitCode)" }
}
$count = 0
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $Destination 'baseline/evidence') -Filter 'default-*.png' -File) {
    $current = Join-Path $Destination ('current/evidence/' + $file.Name)
    if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $current).Hash) { throw "Old-family Application Menu paint changed: $($file.Name)" }
    $count++
}
if ($count -ne 32) { throw "Expected 32 image pairs; found $count." }
@{ BaselineVersion = $BaselinePackageVersion; CurrentVersion = $state.PackageVersion; ExactPairs = $count; Configuration = $Configuration } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Destination 'comparison.json')
Write-Output "PASS: $count byte-exact old-family Application Menu/frame pairs; independent native checks pass."
