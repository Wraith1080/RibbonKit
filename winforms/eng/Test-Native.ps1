param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug', [switch]$Package)
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path $PSScriptRoot
Push-Location $nativeRoot
try {
    dotnet build RibbonKit.WinForms.sln -c $Configuration
    if ($LASTEXITCODE) { throw 'Native build failed.' }
    dotnet test tests/RibbonKit.WinForms.Tests -c $Configuration --no-build
    if ($LASTEXITCODE) { throw 'Native unit/STA tests failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build
    if ($LASTEXITCODE) { throw 'Native input/UIA checks failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --keyboard
    if ($LASTEXITCODE) { throw 'Native keyboard navigation checks failed.' }
    if ($Package) {
        $feed = Join-Path $nativeRoot 'artifacts/packages'
        dotnet pack src/RibbonKit.WinForms -c $Configuration --no-build -o $feed
        if ($LASTEXITCODE) { throw 'Native pack failed.' }
        # Copy to a fresh directory outside the checkout and use an empty package
        # cache. No project references, parent build props or Showcase assets.
        $consumerRoot = Join-Path ([IO.Path]::GetTempPath()) ('RibbonKit-WinForms-consumer-' + [guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $consumerRoot | Out-Null
        Copy-Item -Path tests/RibbonKit.WinForms.Consumer/*.cs, tests/RibbonKit.WinForms.Consumer/*.csproj -Destination $consumerRoot
        dotnet restore (Join-Path $consumerRoot 'RibbonKit.WinForms.Consumer.csproj') --source $feed --packages (Join-Path $consumerRoot 'packages')
        if ($LASTEXITCODE) { throw 'Isolated package restore failed.' }
        dotnet build (Join-Path $consumerRoot 'RibbonKit.WinForms.Consumer.csproj') -c $Configuration --no-restore
        if ($LASTEXITCODE) { throw 'Isolated package consumer build failed.' }
        $consumerExe = Join-Path $consumerRoot "bin/$Configuration/net10.0-windows/RibbonKit.WinForms.Consumer.exe"
        dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --app $consumerExe
        if ($LASTEXITCODE) { throw 'Isolated package consumer UIA/input checks failed.' }
        dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --keyboard --app $consumerExe
        if ($LASTEXITCODE) { throw 'Isolated package consumer keyboard checks failed.' }
        Write-Host "Package consumer evidence retained at $consumerRoot"
    }
} finally { Pop-Location }
