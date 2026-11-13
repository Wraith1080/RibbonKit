param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [string]$PackageVersion,
    [string]$PackageFeed,
    [string]$Destination,
    [switch]$NativeOnly
)
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path $PSScriptRoot -Parent
if (-not $PackageVersion -or -not $PackageFeed) {
    $state = Get-Content -LiteralPath (Join-Path $nativeRoot 'samples/RibbonKit.WinForms.DesignerSample/.local/last-refresh.json') | ConvertFrom-Json
    if (-not $PackageVersion) { $PackageVersion = $state.PackageVersion }
    if (-not $PackageFeed) { $PackageFeed = $state.PackageFeed }
}
if (-not $Destination) {
    $stamp = [DateTime]::UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'")
    $Destination = Join-Path $nativeRoot "artifacts/accent-adaptation/public-$stamp"
}
$Destination = [IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $Destination) { throw "Preserve the existing evidence directory: $Destination" }
$appRoot = Join-Path $Destination 'App'
New-Item -ItemType Directory -Path $appRoot | Out-Null
$fixture = Join-Path $nativeRoot 'tests/RibbonKit.WinForms.Consumer'
Copy-Item -LiteralPath (Join-Path $fixture 'ThemeAccentAdaptationChecks.cs'), (Join-Path $fixture 'app.manifest') -Destination $appRoot
# Isolate the package, cache and imports from project references and sample helpers.
Set-Content -LiteralPath (Join-Path $appRoot 'Directory.Build.props') -Value '<Project />'
Set-Content -LiteralPath (Join-Path $appRoot 'Directory.Build.targets') -Value '<Project />'
$project = Join-Path $appRoot 'AccentConsumer.csproj'
$projectXml = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0-windows</TargetFramework><OutputType>WinExe</OutputType><ApplicationManifest>app.manifest</ApplicationManifest><UseWindowsForms>true</UseWindowsForms><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><PackageReference Include="RibbonKit.WinForms" Version=""/></ItemGroup></Project>'
$projectXml.Project.ItemGroup.PackageReference.Version = $PackageVersion
$projectXml.Save($project)
Set-Content -LiteralPath (Join-Path $appRoot 'Program.cs') -Value 'internal static class Program { [System.STAThread] private static int Main(string[] args) => IndependentConsumer.ThemeAccentAdaptationChecks.Run(args[0], args.Contains("--native-only")); }'
$configPath = Join-Path $appRoot 'NuGet.Config'
$config = [xml]'<configuration><packageSources><clear/><add key="local" value=""/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><config><add key="globalPackagesFolder" value="../packages"/></config></configuration>'
$config.configuration.packageSources.add[0].value = [IO.Path]::GetFullPath($PackageFeed)
$config.Save($configPath)
@{ PackageVersion = $PackageVersion; PackageFeed = $PackageFeed; Configuration = $Configuration; Project = $project } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Destination 'preparation.json')
dotnet restore $project --configfile $configPath
if ($LASTEXITCODE) { throw 'Accent consumer package restore failed.' }
dotnet build $project -c $Configuration --no-restore
if ($LASTEXITCODE) { throw 'Accent consumer build failed.' }
$evidence = Join-Path $Destination 'native'
$executable = Join-Path $appRoot "bin/$Configuration/net10.0-windows/AccentConsumer.exe"
$arguments = @('"' + $evidence + '"')
if ($NativeOnly) { $arguments += '--native-only' }
$process = Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
Get-Content -LiteralPath (Join-Path $evidence 'result.log')
if ($process.ExitCode) { throw 'Public accent adaptation checks failed.' }
Write-Host "Accent evidence: $evidence (physical/VS acceptance remains separate)."
