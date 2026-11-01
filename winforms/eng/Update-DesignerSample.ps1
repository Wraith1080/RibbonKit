param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [switch]$NoBuild
)
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path $PSScriptRoot -Parent
$sampleRoot = Join-Path $nativeRoot 'samples/RibbonKit.WinForms.DesignerSample'
$project = Join-Path $sampleRoot 'RibbonKit.WinForms.DesignerSample.csproj'
$runtime = Join-Path $nativeRoot 'src/RibbonKit.WinForms/RibbonKit.WinForms.csproj'
if (-not (Test-Path -LiteralPath $project)) { throw "The persistent Designer Sample is missing: $project" }

if (-not $NoBuild) {
    dotnet build (Join-Path $nativeRoot 'src/RibbonKit.WinForms.Design/RibbonKit.WinForms.Design.csproj') -c $Configuration
    if ($LASTEXITCODE) { throw 'Native runtime/designer build failed.' }
    dotnet build (Join-Path $nativeRoot 'src/RibbonKit.WinForms.Design.Client/RibbonKit.WinForms.Design.Client.csproj') -c $Configuration
    if ($LASTEXITCODE) { throw 'Native designer client build failed.' }
}

$baseVersion = & dotnet msbuild $runtime -nologo -getProperty:VersionPrefix -property:Configuration=$Configuration
if ($LASTEXITCODE) { throw 'Could not read the native package version.' }
$baseVersion = ($baseVersion -join '').Trim()
if ($baseVersion -notmatch '^\d+\.\d+\.\d+$') { throw "Unexpected native VersionPrefix: $baseVersion" }
# A fresh local package version avoids stale NuGet/VS copies without deleting
# caches or touching the user's forms, resources, project or shared package cache.
$stamp = [DateTime]::UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'", [Globalization.CultureInfo]::InvariantCulture)
$packageVersion = "$baseVersion-designer.local$stamp"
$feed = Join-Path $nativeRoot 'artifacts/designer-sample/feed'
New-Item -ItemType Directory -Path $feed -Force | Out-Null
dotnet pack $runtime -c $Configuration --no-build -o $feed -p:PackageVersion=$packageVersion
if ($LASTEXITCODE) { throw 'Local designer package creation failed.' }

$stateRoot = Join-Path $sampleRoot '.local'
New-Item -ItemType Directory -Path $stateRoot -Force | Out-Null
$props = [xml]'<Project><PropertyGroup><RibbonKitDesignerSamplePackageVersion /></PropertyGroup></Project>'
$props.Project.PropertyGroup.RibbonKitDesignerSamplePackageVersion = $packageVersion
$props.Save((Join-Path $stateRoot 'DesignerPackage.props'))
dotnet restore $project --configfile (Join-Path $sampleRoot 'NuGet.Config')
if ($LASTEXITCODE) { throw 'Designer Sample package restore failed.' }
dotnet build $project -c $Configuration --no-restore
if ($LASTEXITCODE) { throw 'Designer Sample build failed; authored files have not been replaced.' }

@{ Project = $project; Configuration = $Configuration; PackageVersion = $packageVersion;
   PackageFeed = $feed; PackageCache = (Join-Path $nativeRoot 'artifacts/designer-sample/packages') } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stateRoot 'last-refresh.json')
Write-Host "Designer Sample updated: $(Join-Path $sampleRoot 'RibbonKit.WinForms.DesignerSample.sln')"
Write-Host "Local package: RibbonKit.WinForms $packageVersion"
Write-Host 'Reopen the solution in Visual Studio to load the refreshed designer. Authored files were not replaced.'
