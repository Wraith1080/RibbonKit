param(
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug',
    [string]$Destination,
    [switch]$HeaderOnly
)
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path $PSScriptRoot -Parent
$state = Get-Content -LiteralPath (Join-Path $nativeRoot 'samples/RibbonKit.WinForms.DesignerSample/.local/last-refresh.json') -Raw | ConvertFrom-Json
if (-not $Destination) { $Destination = Join-Path $nativeRoot ('artifacts/office2010-f3/public-' + [DateTime]::UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'")) }
$Destination = [IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $Destination) { throw "Preserve existing evidence: $Destination" }
$app = Join-Path $Destination 'App'; New-Item -ItemType Directory -Path $app -Force | Out-Null
$checkClass = if ($HeaderOnly) { 'HeaderRefinementChecks' } else { 'Office2010ShellChecks' }
Copy-Item -LiteralPath (Join-Path $nativeRoot ('tests/RibbonKit.WinForms.Consumer/' + $checkClass + '.cs')), (Join-Path $nativeRoot 'tests/RibbonKit.WinForms.Consumer/app.manifest') -Destination $app
Set-Content -LiteralPath (Join-Path $app 'Directory.Build.props') -Value '<Project />'
Set-Content -LiteralPath (Join-Path $app 'Directory.Build.targets') -Value '<Project />'
Set-Content -LiteralPath (Join-Path $app 'Program.cs') -Value ('internal static class Program { [System.STAThread] private static int Main(string[] args) => IndependentConsumer.' + $checkClass + '.Run(args[0]); }')
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>WinExe</OutputType><TargetFramework>net10.0-windows</TargetFramework><UseWindowsForms>true</UseWindowsForms><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><ApplicationManifest>app.manifest</ApplicationManifest></PropertyGroup><ItemGroup><PackageReference Include="RibbonKit.WinForms" Version="" /></ItemGroup></Project>'
$project.Project.ItemGroup.PackageReference.Version = $state.PackageVersion
$projectPath = Join-Path $app 'Office2010ShellConsumer.csproj'; $project.Save($projectPath)
$config = [xml]'<configuration><packageSources><clear/><add key="local" value=""/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><config><add key="globalPackagesFolder" value="../packages"/></config></configuration>'
$config.configuration.packageSources.add[0].value = $state.PackageFeed
$configPath = Join-Path $app 'NuGet.Config'; $config.Save($configPath)
@{ PackageVersion = $state.PackageVersion; Configuration = $Configuration; Project = $projectPath; CheckClass = $checkClass } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Destination 'preparation.json')
dotnet restore $projectPath --configfile $configPath
if ($LASTEXITCODE) { throw 'Office 2010 package restore failed.' }
dotnet build $projectPath -c $Configuration --no-restore
if ($LASTEXITCODE) { throw 'Office 2010 public consumer build failed.' }
$output = Join-Path $Destination 'evidence'
$process = Start-Process -FilePath (Join-Path $app "bin/$Configuration/net10.0-windows/Office2010ShellConsumer.exe") -ArgumentList ('"' + $output + '"') -WindowStyle Hidden -PassThru
if (-not $process.WaitForExit(45000)) { Stop-Process -Id $process.Id; throw 'Office 2010 public consumer timed out.' }
foreach ($name in @('result.log','failure.log')) { $log = Join-Path $output $name; if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log } }
if ($process.ExitCode) { throw 'Office 2010 public consumer failed.' }
