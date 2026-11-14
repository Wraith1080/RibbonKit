param(
    [string]$Check = '--verify-clean-sample',
    [string]$Output,
    [string[]]$CheckArguments = @(),
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [ValidateRange(1, 3600)][int]$TimeoutSeconds = 120,
    [switch]$NoBuild,
    [switch]$List
)
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path $PSScriptRoot -Parent
$testRoot = Join-Path $nativeRoot 'tests/RibbonKit.WinForms.DesignerSample.Tests'
$program = Get-Content -LiteralPath (Join-Path $testRoot 'Program.cs') -Raw
$choices = @([regex]::Matches($program, '"(--(?:verify|profile|trace|generate|capture)-[a-z0-9-]+)"') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
if ($List) { $choices; return }
if ($Check -notin $choices) { throw "Unknown check '$Check'. Use -List; retired review-tab checks are no longer available." }
if (-not $Output) {
    $stamp = [DateTime]::UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'")
    $Output = Join-Path $nativeRoot ("artifacts/designer-sample-checks/" + $Check.TrimStart('-') + '-' + $stamp)
}
$Output = [IO.Path]::GetFullPath($Output)
if (-not $NoBuild) {
    dotnet build (Join-Path $nativeRoot 'samples/RibbonKit.WinForms.DesignerSample/RibbonKit.WinForms.DesignerSample.csproj') -c $Configuration
    if ($LASTEXITCODE) { throw 'Normal Designer Sample build failed.' }
    dotnet build (Join-Path $testRoot 'RibbonKit.WinForms.DesignerSample.Tests.csproj') -c $Configuration
    if ($LASTEXITCODE) { throw 'Designer Sample test host build failed.' }
}
$executable = Join-Path $testRoot "bin/$Configuration/net10.0-windows/RibbonKit.WinForms.DesignerSample.Tests.exe"
if (-not (Test-Path -LiteralPath $executable)) { throw 'Build the test host before using -NoBuild.' }
if ($Check -eq '--verify-sample-isolation' -and $CheckArguments.Count -eq 0) {
    $CheckArguments = @(Join-Path $nativeRoot "samples/RibbonKit.WinForms.DesignerSample/bin/$Configuration/net10.0-windows/RibbonKit.WinForms.DesignerSample.dll")
}
$arguments = @($Check, $Output) + $CheckArguments
# Start-Process joins its argument array; quote each Windows process argument.
function QuoteArgument([string]$value) {
    $escaped = [regex]::Replace($value, '(\\*)"', '$1$1\"')
    $escaped = [regex]::Replace($escaped, '(\\+)$', '$1$1')
    return '"' + $escaped + '"'
}
$process = Start-Process -FilePath $executable -ArgumentList (($arguments | ForEach-Object { QuoteArgument $_ }) -join ' ') -WindowStyle Hidden -PassThru
if (-not $process.WaitForExit($TimeoutSeconds * 1000)) { Stop-Process -Id $process.Id; throw "Designer Sample check timed out: $Check" }
foreach ($name in @('result.log', 'failure.log')) { $log = Join-Path $Output $name; if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log } }
if ($process.ExitCode) { throw "Designer Sample check failed ($($process.ExitCode)): $Check. Evidence: $Output" }
Write-Host "Designer Sample evidence: $Output (actual VS/physical acceptance remains separate)."
