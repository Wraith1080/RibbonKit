param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [string]$Destination,
    [switch]$NoBuild
)
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path $PSScriptRoot -Parent
if (-not $Destination) {
    $Destination = Join-Path ([IO.Path]::GetTempPath()) ('RibbonKit-WinForms-Designer-' + [guid]::NewGuid().ToString('N'))
}
$Destination = [IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $Destination) { throw "Destination already exists; preserve it and choose a new directory: $Destination" }
$appRoot = Join-Path $Destination 'App'
$feed = Join-Path $Destination 'feed'
New-Item -ItemType Directory -Path $appRoot, $feed | Out-Null
if (-not $NoBuild) {
    dotnet build (Join-Path $nativeRoot 'src/RibbonKit.WinForms.Design/RibbonKit.WinForms.Design.csproj') -c $Configuration
    if ($LASTEXITCODE) { throw 'Native runtime/designer build failed.' }
    dotnet build (Join-Path $nativeRoot 'src/RibbonKit.WinForms.Design.Client/RibbonKit.WinForms.Design.Client.csproj') -c $Configuration
    if ($LASTEXITCODE) { throw 'Native designer client build failed.' }
}
dotnet pack (Join-Path $nativeRoot 'src/RibbonKit.WinForms/RibbonKit.WinForms.csproj') -c $Configuration --no-build -o $feed
if ($LASTEXITCODE) { throw 'Local native pack failed.' }
$fixture = Join-Path $nativeRoot 'tests/RibbonKit.WinForms.DesignerConsumer'
Get-ChildItem -LiteralPath $fixture -File | Where-Object Extension -In '.cs', '.csproj', '.resx' | Copy-Item -Destination $appRoot
Copy-Item -LiteralPath (Join-Path $fixture 'Assets') -Destination $appRoot -Recurse
$config = [xml]'<configuration><packageSources><clear/><add key="local" value=""/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><config><add key="globalPackagesFolder" value="../packages"/></config></configuration>'
$config.configuration.packageSources.add[0].value = $feed
$config.Save((Join-Path $appRoot 'NuGet.Config'))
$project = Join-Path $appRoot 'RibbonKit.WinForms.DesignerConsumer.csproj'
dotnet restore $project --configfile (Join-Path $appRoot 'NuGet.Config')
if ($LASTEXITCODE) { throw 'Isolated designer consumer restore failed.' }
dotnet build $project -c $Configuration --no-restore
if ($LASTEXITCODE) { throw 'Isolated designer consumer build failed.' }
dotnet new sln --format sln -n RibbonKit.Designer.Check -o $appRoot
if ($LASTEXITCODE) { throw 'Independent solution creation failed.' }
$solution = Join-Path $appRoot 'RibbonKit.Designer.Check.sln'
dotnet sln $solution add $project
if ($LASTEXITCODE) { throw 'Independent solution project registration failed.' }
@{ Root = $Destination; Project = $project; Solution = $solution; Configuration = $Configuration; PackageCache = (Join-Path $Destination 'packages') } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Destination 'preparation.json')
Write-Host "Designer consumer prepared: $solution"
Write-Host "No global package cache or reference-project file was changed. Evidence root: $Destination"
