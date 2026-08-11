[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version = '1.0.0',
    [switch]$SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$releaseDirectory = Join-Path $repositoryRoot "artifacts\github-release-v$Version"
$releaseNotes = Join-Path $repositoryRoot 'RELEASE_NOTES.md'
$packageProject = Join-Path $repositoryRoot 'src\RibbonKit\RibbonKit.csproj'
$packageValidator = Join-Path $PSScriptRoot 'Validate-Package.ps1'

function Invoke-DotNet {
    param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $releaseNotes -PathType Leaf)) {
    throw "Release notes were not found at '$releaseNotes'."
}

if (Test-Path -LiteralPath $releaseDirectory) {
    $resolvedArtifacts = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts'))
    $resolvedRelease = [System.IO.Path]::GetFullPath($releaseDirectory)
    if (-not $resolvedRelease.StartsWith($resolvedArtifacts + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean release directory outside '$resolvedArtifacts'."
    }

    Remove-Item -LiteralPath $resolvedRelease -Recurse -Force
}

New-Item -ItemType Directory -Path $releaseDirectory -Force | Out-Null

Push-Location $repositoryRoot
try {
    Invoke-DotNet @('restore', 'RibbonKit.sln')
    Invoke-DotNet @('build', 'RibbonKit.sln', '--no-restore', '--configuration', 'Release', "-p:Version=$Version")

    if (-not $SkipTests) {
        Invoke-DotNet @('test', 'RibbonKit.sln', '--no-build', '--configuration', 'Release', '--verbosity', 'normal', "-p:Version=$Version")
    }

    Invoke-DotNet @(
        'pack',
        $packageProject,
        '--no-build',
        '--configuration', 'Release',
        '--output', $releaseDirectory,
        "-p:Version=$Version"
    )

    & $packageValidator -PackageDirectory $releaseDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "Package validation failed with exit code $LASTEXITCODE."
    }

    Copy-Item -LiteralPath $releaseNotes -Destination (Join-Path $releaseDirectory 'RELEASE_NOTES.md')

    $releaseAssets = @(Get-ChildItem -LiteralPath $releaseDirectory -File |
        Where-Object Extension -in @('.nupkg', '.snupkg') |
        Sort-Object Name)
    if ($releaseAssets.Count -ne 2) {
        throw "Expected one main package and one symbol package; found $($releaseAssets.Count) assets."
    }

    $checksumLines = @($releaseAssets | ForEach-Object {
        $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $($_.Name)"
    })
    [System.IO.File]::WriteAllLines(
        (Join-Path $releaseDirectory 'SHA256SUMS.txt'),
        $checksumLines,
        (New-Object System.Text.UTF8Encoding($false)))

    Write-Host "Prepared local GitHub Release candidate in '$releaseDirectory'."
    Get-ChildItem -LiteralPath $releaseDirectory -File |
        Sort-Object Name |
        Select-Object Name, Length, LastWriteTime
    Write-Warning 'Nothing was uploaded. After committing the release changes, rerun this script so Source Link and repository metadata point to that exact release commit.'
}
finally {
    Pop-Location
}
