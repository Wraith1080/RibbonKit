<#
.SYNOPSIS
Runs an explicitly selected local regression slice, using normal build outputs.
.DESCRIPTION
Runtime and Writer use a dotnet test filter. Visual accepts snapshot names or
wildcards. Portability uses the independent consumer's existing focused scopes.
Selection and snapshot-update environment variables are restored after the run.
Use dotnet test RibbonKit.sln for full validation, with local scopes cleared.
.EXAMPLE
./eng/Test-Focused.ps1 -Suite Runtime -Filter 'FullyQualifiedName~GalleryRowScrollingTests'
.EXAMPLE
./eng/Test-Focused.ps1 -Suite Visual -Scenes 'crystal-*-qat-*' -Configuration Release
.EXAMPLE
./eng/Test-Focused.ps1 -Suite Portability -Scope GroupedGalleries
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Runtime', 'Writer', 'Visual', 'Portability')]
    [string]$Suite,
    [string]$Filter,
    [string[]]$Scenes,
    [ValidateSet('SplitButtonDensity', 'StackedCustomGroups', 'NativeGalleryPopup',
        'GalleryQuickAccess', 'GalleryQuickAccessKeyboard', 'GalleryQuickAccessVisible',
        'ComboQuickAccess', 'ComboQuickAccessKeyboard', 'GroupedGalleries',
        'GalleryHeadersAndQatCommands', 'DensitySelector', 'MessageTransition',
        'QatThemeRestore', 'MinimizedDivider', 'TouchChrome', 'GroupLauncher',
        'DensityTransition', 'GlassOverlay', 'BackstageCaption', 'Touch', IgnoreCase = $false)]
    [string]$Scope,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectName = switch ($Suite) {
    'Runtime' { 'RibbonKit.Tests' }
    'Writer' { 'RibbonKit.Writer.Tests' }
    'Visual' { 'RibbonKit.VisualTests' }
    'Portability' { 'RibbonKit.Portability.Tests' }
}

switch ($Suite) {
    { $_ -in 'Runtime', 'Writer' } {
        if ([string]::IsNullOrWhiteSpace($Filter) -or $Scenes -or $Scope) {
            throw "$Suite requires -Filter and does not accept -Scenes or -Scope."
        }
        $testFilter = $Filter
        $selection = $Filter
    }
    'Visual' {
        if (-not $Scenes -or @($Scenes | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -gt 0 -or $Filter -or $Scope) {
            throw 'Visual requires nonempty -Scenes and does not accept -Filter or -Scope.'
        }
        $testFilter = 'FullyQualifiedName~VisualSnapshotTests.Every_approved_office_theme_variant_and_dpi_matches_its_snapshot'
        $selection = $Scenes -join ','
    }
    'Portability' {
        if (-not $Scope -or $Filter -or $Scenes) {
            throw 'Portability requires -Scope and does not accept -Filter or -Scenes.'
        }
        $testFilter = 'FullyQualifiedName~ApplicationButtonShapeThemeTests.Theme_default_and_local_value_follow_wpf_precedence'
        $selection = $Scope
    }
}

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectPath = Join-Path $repositoryRoot "tests\$projectName\$projectName.csproj"
$selectionVariables = @('RIBBONKIT_VISUAL_SCENES', 'RIBBONKIT_PORTABILITY_SCOPE',
    'RIBBONKIT_CAPTURE_SNAPSHOTS', 'RIBBONKIT_UPDATE_SNAPSHOTS')
$previousValues = @{}
foreach ($variableName in $selectionVariables) {
    $previousValues[$variableName] = [Environment]::GetEnvironmentVariable($variableName, 'Process')
}

Push-Location $repositoryRoot
try {
    foreach ($variableName in $selectionVariables) {
        [Environment]::SetEnvironmentVariable($variableName, $null, 'Process')
    }
    if ($Suite -eq 'Visual') {
        $env:RIBBONKIT_VISUAL_SCENES = $selection
    }
    if ($Suite -eq 'Portability') {
        $env:RIBBONKIT_PORTABILITY_SCOPE = $Scope
    }

    Write-Host "Focused $Suite validation ($Configuration): $selection"
    $logger = if ($Suite -eq 'Visual') { 'console;verbosity=detailed' } else { 'console;verbosity=normal' }
    & dotnet test $projectPath --configuration $Configuration --filter $testFilter `
        --logger $logger -- RunConfiguration.TreatNoTestsAsError=true
    if ($LASTEXITCODE -ne 0) {
        throw "Focused $Suite validation failed (exit code $LASTEXITCODE)."
    }
}
finally {
    foreach ($variableName in $selectionVariables) {
        [Environment]::SetEnvironmentVariable($variableName, $previousValues[$variableName], 'Process')
    }
    Pop-Location
}
