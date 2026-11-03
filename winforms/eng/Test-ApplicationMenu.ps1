param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [string[]]$Modes = @('Office-Solid-Compact', 'Office-Solid-Touch', 'Office-Mica-Compact', 'Office-Mica-Touch', 'Office-Acrylic-Compact', 'Office-Acrylic-Touch',
        'Flat-Solid-Compact', 'Flat-Solid-Touch', 'Flat-Mica-Compact', 'Flat-Mica-Touch', 'Flat-Acrylic-Compact', 'Flat-Acrylic-Touch')
)
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path $PSScriptRoot -Parent
$refresh = Get-Content -LiteralPath (Join-Path $nativeRoot 'samples/RibbonKit.WinForms.DesignerSample/.local/last-refresh.json') | ConvertFrom-Json
if ($refresh.Configuration -ne $Configuration) { throw 'Refresh the permanent Designer Sample in the requested configuration first.' }
$evidence = Join-Path $nativeRoot 'artifacts/application-menu'
New-Item -ItemType Directory -Path $evidence -Force | Out-Null
$consumerRoot = Join-Path ([IO.Path]::GetTempPath()) ('RibbonKit-ApplicationMenu-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $consumerRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $nativeRoot 'tests/RibbonKit.WinForms.Consumer/ApplicationMenuChecks.cs'), (Join-Path $nativeRoot 'tests/RibbonKit.WinForms.Consumer/app.manifest') -Destination $consumerRoot
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0-windows</TargetFramework><OutputType>WinExe</OutputType><UseWindowsForms>true</UseWindowsForms><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><ApplicationManifest>app.manifest</ApplicationManifest></PropertyGroup><ItemGroup><PackageReference Include="RibbonKit.WinForms" Version=""/></ItemGroup></Project>'
$project.Project.ItemGroup.PackageReference.Version = $refresh.PackageVersion
$projectPath = Join-Path $consumerRoot 'ApplicationMenu.Consumer.csproj'
$project.Save($projectPath)
Set-Content -LiteralPath (Join-Path $consumerRoot 'Program.cs') -Value 'namespace IndependentConsumer; internal static class Program { [System.STAThread] private static int Main(string[] args) => ApplicationMenuChecks.Run(args[0], args[1]); }'
$config = [xml]'<configuration><packageSources><clear/><add key="local" value=""/><add key="nuget" value="https://api.nuget.org/v3/index.json"/></packageSources></configuration>'
$config.configuration.packageSources.add[0].value = $refresh.PackageFeed
$configPath = Join-Path $consumerRoot 'NuGet.Config'; $config.Save($configPath)
Set-Content -LiteralPath (Join-Path $evidence 'consumer-path.txt') -Value $consumerRoot
dotnet restore $projectPath --configfile $configPath --packages (Join-Path $consumerRoot 'packages')
if ($LASTEXITCODE) { throw 'Isolated Application Menu package restore failed.' }
dotnet build $projectPath -c $Configuration --no-restore
if ($LASTEXITCODE) { throw 'Public-only Application Menu consumer build failed.' }
$package = Join-Path $refresh.PackageFeed ('RibbonKit.WinForms.' + $refresh.PackageVersion + '.nupkg')
$archive = [IO.Compression.ZipFile]::OpenRead($package)
try {
    $entries = @($archive.Entries.FullName)
    foreach ($suffix in @('/RibbonKit.WinForms.dll', '/RibbonKit.WinForms.xml', '/Server/RibbonKit.WinForms.Design.dll', '/RibbonKit.WinForms.Design.Client.dll', '/Server/RibbonKit.WinForms.Design.Protocol.dll', '/WinForms/RibbonKit.WinForms.Design.Protocol.dll')) {
        if (-not ($entries | Where-Object { $_.EndsWith($suffix) })) { throw "Missing package entry: $suffix" }
    }
    $entries | Set-Content -LiteralPath (Join-Path $evidence 'package-contents.txt')
} finally { $archive.Dispose() }
$consumerDll = Join-Path $consumerRoot "bin/$Configuration/net10.0-windows/ApplicationMenu.Consumer.dll"
foreach ($mode in $Modes) {
    if ($mode -notmatch '^(Office|Flat)-(Solid|Mica|Acrylic)-(Compact|Touch)$') { throw "Unknown native scope: $mode" }
    $target = Join-Path $evidence ('native/' + $mode)
    # dotnet waits for this GUI fixture to exit. Never overlap foreground scopes.
    dotnet $consumerDll $target $mode
    if ($LASTEXITCODE) { throw "Application Menu scope failed: $target/result.log" }
    Write-Output "PASS: $mode"
}
Write-Output "Package $($refresh.PackageVersion); public-only consumer: $consumerRoot"
