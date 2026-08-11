[CmdletBinding()]
param(
    [string]$PackageDirectory = (Join-Path $PSScriptRoot '..\artifacts'),
    [switch]$KeepConsumer
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression.FileSystem

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$packageDirectoryPath = [System.IO.Path]::GetFullPath($PackageDirectory)

function Assert-Condition {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Get-ZipEntryNames {
    param([string]$Path)

    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        return @($archive.Entries | ForEach-Object { $_.FullName })
    }
    finally {
        $archive.Dispose()
    }
}

function Read-ZipTextEntry {
    param(
        [string]$Path,
        [string]$EntryName
    )

    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entry = $archive.GetEntry($EntryName)
        Assert-Condition ($null -ne $entry) "Package '$Path' is missing '$EntryName'."
        $reader = New-Object System.IO.StreamReader($entry.Open())
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Assert-ArchiveLayout {
    param(
        [string]$Path,
        [string[]]$RequiredEntries
    )

    $entryNames = @(Get-ZipEntryNames $Path)
    $missingEntries = @($RequiredEntries | Where-Object { $entryNames -notcontains $_ })
    Assert-Condition ($missingEntries.Count -eq 0) "Package '$Path' is missing: $($missingEntries -join ', ')."

    $duplicateEntries = @($entryNames | Group-Object | Where-Object Count -gt 1 | ForEach-Object Name)
    Assert-Condition ($duplicateEntries.Count -eq 0) "Package '$Path' has duplicate entries: $($duplicateEntries -join ', ')."

    $allowedInfrastructureEntries = @('_rels/.rels', '[Content_Types].xml')
    $unexpectedEntries = @($entryNames | Where-Object {
        $RequiredEntries -notcontains $_ -and
        $allowedInfrastructureEntries -notcontains $_ -and
        $_ -notmatch '^package/services/metadata/core-properties/[0-9a-f]+\.psmdcp$'
    })
    Assert-Condition ($unexpectedEntries.Count -eq 0) "Package '$Path' has unexpected entries: $($unexpectedEntries -join ', ')."
}

function Write-Utf8File {
    param(
        [string]$Path,
        [string]$Content
    )

    $utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8WithoutBom)
}

function Invoke-DotNet {
    param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

Assert-Condition (Test-Path -LiteralPath $packageDirectoryPath -PathType Container) "Package directory '$packageDirectoryPath' does not exist."

$mainPackages = @(Get-ChildItem -LiteralPath $packageDirectoryPath -Filter 'RibbonKit.*.nupkg' -File |
    Where-Object Extension -eq '.nupkg')
$symbolPackages = @(Get-ChildItem -LiteralPath $packageDirectoryPath -Filter 'RibbonKit.*.snupkg' -File)

Assert-Condition ($mainPackages.Count -eq 1) "Expected exactly one RibbonKit .nupkg in '$packageDirectoryPath'; found $($mainPackages.Count)."
Assert-Condition ($symbolPackages.Count -eq 1) "Expected exactly one RibbonKit .snupkg in '$packageDirectoryPath'; found $($symbolPackages.Count)."

$mainPackage = $mainPackages[0]
$symbolPackage = $symbolPackages[0]

$runtimeTfms = @('net8.0-windows7.0', 'net9.0-windows7.0')
$mainRequiredEntries = @(
    'RibbonKit.nuspec',
    'README.md',
    'RibbonKit.png',
    'tools/VisualStudioToolsManifest.xml'
)
$symbolRequiredEntries = @('RibbonKit.nuspec')

foreach ($tfm in $runtimeTfms) {
    $mainRequiredEntries += @(
        "lib/$tfm/RibbonKit.dll",
        "lib/$tfm/RibbonKit.xml",
        "lib/$tfm/Design/RibbonKit.DesignTools.dll"
    )
    $symbolRequiredEntries += "lib/$tfm/RibbonKit.pdb"
}

Assert-ArchiveLayout $mainPackage.FullName $mainRequiredEntries
Assert-ArchiveLayout $symbolPackage.FullName $symbolRequiredEntries

$mainEntries = @(Get-ZipEntryNames $mainPackage.FullName)
Assert-Condition (-not ($mainEntries | Where-Object { $_ -like '*.pdb' })) 'The main package must not duplicate symbol PDBs.'
Assert-Condition (-not ($mainEntries | Where-Object { $_ -like '*.cs' })) 'The main package unexpectedly contains source files.'

[xml]$nuspec = Read-ZipTextEntry $mainPackage.FullName 'RibbonKit.nuspec'
$metadata = $nuspec.package.metadata
$packageVersion = [string]$metadata.version

Assert-Condition ([string]$metadata.id -eq 'RibbonKit') 'NuSpec package ID must be RibbonKit.'
Assert-Condition (-not [string]::IsNullOrWhiteSpace($packageVersion)) 'NuSpec package version is missing.'
Assert-Condition ([string]$metadata.license.InnerText -eq 'MIT') 'NuSpec license must be MIT.'
Assert-Condition ([string]$metadata.license.type -eq 'expression') 'NuSpec license must use an SPDX expression.'
Assert-Condition ([string]$metadata.readme -eq 'README.md') 'NuSpec readme path is incorrect.'
Assert-Condition ([string]$metadata.icon -eq 'RibbonKit.png') 'NuSpec icon path is incorrect.'
Assert-Condition ([string]$metadata.repository.type -eq 'git') 'NuSpec repository type must be git.'
Assert-Condition ([string]$metadata.repository.url -eq 'https://github.com/Wraith1080/RibbonKit') 'NuSpec repository URL is incorrect.'
Assert-Condition ([string]$metadata.repository.commit -match '^[0-9a-f]{40}$') 'NuSpec repository commit must be a full Git SHA.'

$dependencyGroups = @($metadata.dependencies.group)
$dependencyTfms = @($dependencyGroups | ForEach-Object { [string]$_.targetFramework })
Assert-Condition ($dependencyGroups.Count -eq 2) 'NuSpec must contain exactly two dependency groups.'
foreach ($tfm in $runtimeTfms) {
    Assert-Condition ($dependencyTfms -contains $tfm) "NuSpec is missing the '$tfm' dependency group."
}
$declaredDependencies = @($dependencyGroups | ForEach-Object {
    $_.ChildNodes | Where-Object LocalName -eq 'dependency'
})
Assert-Condition ($declaredDependencies.Count -eq 0) 'RibbonKit unexpectedly exposes package dependencies to consumers.'

[xml]$symbolNuspec = Read-ZipTextEntry $symbolPackage.FullName 'RibbonKit.nuspec'
Assert-Condition ([string]$symbolNuspec.package.metadata.version -eq $packageVersion) 'Main and symbol package versions do not match.'
Assert-Condition ($mainPackage.BaseName -eq "RibbonKit.$packageVersion") 'Main package filename does not match its NuSpec version.'
Assert-Condition ($symbolPackage.BaseName -eq "RibbonKit.$packageVersion") 'Symbol package filename does not match its NuSpec version.'

[xml]$toolboxManifest = Read-ZipTextEntry $mainPackage.FullName 'tools/VisualStudioToolsManifest.xml'
$toolboxItems = @($toolboxManifest.FileList.File.ToolboxItems.Item)
Assert-Condition ($toolboxItems.Count -gt 0) 'The packaged Visual Studio toolbox manifest contains no controls.'
Assert-Condition (@($toolboxItems | Where-Object Type -eq 'RibbonKit.Controls.Ribbon').Count -eq 1) 'The toolbox manifest must expose RibbonKit.Controls.Ribbon.'

$consumerRoot = Join-Path $repositoryRoot 'TestResults\package-consumer'
$consumerProject = Join-Path $consumerRoot 'PackageConsumer.csproj'
$consumerPackages = Join-Path $consumerRoot '.packages'
$consumerSucceeded = $false

if (Test-Path -LiteralPath $consumerRoot) {
    Remove-Item -LiteralPath $consumerRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $consumerRoot -Force | Out-Null

$escapedPackageDirectory = [System.Security.SecurityElement]::Escape($packageDirectoryPath)
$escapedPackageVersion = [System.Security.SecurityElement]::Escape($packageVersion)

Write-Utf8File (Join-Path $consumerRoot 'NuGet.Config') @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="RibbonKit local package" value="$escapedPackageDirectory" />
  </packageSources>
</configuration>
"@

Write-Utf8File $consumerProject @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0-windows;net9.0-windows</TargetFrameworks>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="RibbonKit" Version="$escapedPackageVersion" />
  </ItemGroup>
</Project>
"@

Write-Utf8File (Join-Path $consumerRoot 'ConsumerWindow.xaml') @'
<rk:RibbonWindow x:Class="PackageConsumer.ConsumerWindow"
                 xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                 xmlns:rk="urn:ribbonkit"
                 Title="Package consumer">
  <rk:Ribbon>
    <rk:RibbonTab Header="Home">
      <rk:RibbonGroup Header="Clipboard">
        <rk:RibbonButton Header="Paste" Size="Large" />
      </rk:RibbonGroup>
    </rk:RibbonTab>
  </rk:Ribbon>
</rk:RibbonWindow>
'@

Write-Utf8File (Join-Path $consumerRoot 'ConsumerWindow.xaml.cs') @'
using RibbonKit.Controls;

namespace PackageConsumer;

public partial class ConsumerWindow : RibbonWindow
{
    public ConsumerWindow()
    {
        InitializeComponent();
    }
}
'@

try {
    Invoke-DotNet @(
        'restore', $consumerProject,
        '--configfile', (Join-Path $consumerRoot 'NuGet.Config'),
        '--packages', $consumerPackages,
        '--force',
        '--no-cache'
    )
    Invoke-DotNet @(
        'build', $consumerProject,
        '--configuration', 'Release',
        '--no-restore',
        "-p:RestorePackagesPath=$consumerPackages"
    )

    foreach ($tfm in @('net8.0-windows', 'net9.0-windows')) {
        $consumerAssembly = Join-Path $consumerRoot "bin\Release\$tfm\PackageConsumer.dll"
        Assert-Condition (Test-Path -LiteralPath $consumerAssembly -PathType Leaf) "Clean consumer did not produce '$consumerAssembly'."
    }

    $consumerSucceeded = $true
}
finally {
    if ($consumerSucceeded -and -not $KeepConsumer -and (Test-Path -LiteralPath $consumerRoot)) {
        Remove-Item -LiteralPath $consumerRoot -Recurse -Force
    }
}

Write-Host "Validated RibbonKit $packageVersion package contents and clean net8/net9 WPF consumption."
