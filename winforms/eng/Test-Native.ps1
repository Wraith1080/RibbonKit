param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug', [switch]$Package)
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path $PSScriptRoot
Push-Location $nativeRoot
try {
    dotnet build RibbonKit.WinForms.sln -c $Configuration
    if ($LASTEXITCODE) { throw 'Native build failed.' }
    dotnet test tests/RibbonKit.WinForms.Tests -c $Configuration --no-build
    if ($LASTEXITCODE) { throw 'Native unit/STA tests failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --minimize-checks TestResults/visual/minimize-native
    if ($LASTEXITCODE) { throw 'Native minimize checks failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --minimize-checks-flat TestResults/visual/minimize-flat-native
    if ($LASTEXITCODE) { throw 'Native neutral minimize checks failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --menu-checks TestResults/visual/menus-native
    if ($LASTEXITCODE) { throw 'Native rich-menu checks failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --menu-checks-flat TestResults/visual/menus-flat-native
    if ($LASTEXITCODE) { throw 'Native neutral rich-menu checks failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --screentip-checks TestResults/visual/screentips-native
    if ($LASTEXITCODE) { throw 'Native ScreenTip hover/lifetime/focus checks failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --screentip-checks-flat TestResults/visual/screentips-flat-native
    if ($LASTEXITCODE) { throw 'Native neutral ScreenTip checks failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --option-checks TestResults/visual/m2d-native
    if ($LASTEXITCODE) { throw 'Native check/radio interaction checks failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --option-checks-flat TestResults/visual/m2d-flat-native
    if ($LASTEXITCODE) { throw 'Native neutral check/radio interaction checks failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --combo-checks TestResults/visual/m2c-native
    if ($LASTEXITCODE) { throw 'Native ComboBox input/binding/ownership checks failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --combo-checks-flat TestResults/visual/m2c-flat-native
    if ($LASTEXITCODE) { throw 'Native neutral ComboBox presentation/input checks failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --editor-checks TestResults/visual/m2b-native
    if ($LASTEXITCODE) { throw 'Native editor input/binding/ownership checks failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --split-checks TestResults/visual/m2a-native
    if ($LASTEXITCODE) { throw 'Native split-button structure/input checks failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --split-checks-flat TestResults/visual/renderer-flat-native
    if ($LASTEXITCODE) { throw 'Native flat-renderer interaction checks failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build
    if ($LASTEXITCODE) { throw 'Native input/UIA checks failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --keyboard
    if ($LASTEXITCODE) { throw 'Native keyboard navigation checks failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --focus-cues TestResults/visual/focus-test-host
    if ($LASTEXITCODE) { throw 'Native keyboard focus appearance checks failed.' }
    dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --popup-dismissal
    if ($LASTEXITCODE) { throw 'Native popup activation/dismissal checks failed.' }
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
        foreach ($minimizeMode in @('minimize', 'minimize-flat')) {
            $minimizeEvidence = Join-Path $nativeRoot ('TestResults/visual/m2g-package-' + $minimizeMode)
            $minimizeProcess = Start-Process -FilePath $consumerExe -ArgumentList @(('--validate-' + $minimizeMode), ('"' + $minimizeEvidence + '"')) -PassThru -Wait
            if ($minimizeProcess.ExitCode) { throw "Package minimize checks failed: $minimizeEvidence/result.log" }
        }
        foreach ($menuMode in @('menus', 'menus-flat')) {
            $menuEvidence = Join-Path $nativeRoot ('TestResults/visual/m2f-package-' + $menuMode)
            $menuProcess = Start-Process -FilePath $consumerExe -ArgumentList @(('--validate-' + $menuMode), ('"' + $menuEvidence + '"')) -PassThru -Wait
            if ($menuProcess.ExitCode) { throw "Package rich-menu checks failed: $menuEvidence/result.log" }
        }
        foreach ($tipMode in @('screentips', 'screentips-flat')) {
            $tipEvidence = Join-Path $nativeRoot ('TestResults/visual/m2e-package-' + $tipMode)
            $tipProcess = Start-Process -FilePath $consumerExe -ArgumentList @(('--validate-' + $tipMode), ('"' + $tipEvidence + '"')) -PassThru -Wait
            if ($tipProcess.ExitCode) { throw "Package ScreenTip checks failed: $tipEvidence/result.log" }
        }
        foreach ($optionMode in @('options', 'options-flat')) {
            $optionEvidence = Join-Path $nativeRoot ('TestResults/visual/m2d-package-' + $optionMode)
            $optionProcess = Start-Process -FilePath $consumerExe -ArgumentList @(('--validate-' + $optionMode), ('"' + $optionEvidence + '"')) -PassThru -Wait
            if ($optionProcess.ExitCode) { throw "Package option checks failed: $optionEvidence/result.log" }
        }
        $comboEvidence = Join-Path $nativeRoot 'TestResults/visual/m2c-package-consumer'
        $comboProcess = Start-Process -FilePath $consumerExe -ArgumentList @('--validate-combo', ('"' + $comboEvidence + '"')) -PassThru -Wait
        if ($comboProcess.ExitCode) { throw "Package ComboBox checks failed: $comboEvidence/result.log" }
        $comboFlatEvidence = Join-Path $nativeRoot 'TestResults/visual/m2c-flat-package-consumer'
        $comboFlatProcess = Start-Process -FilePath $consumerExe -ArgumentList @('--validate-combo-flat', ('"' + $comboFlatEvidence + '"')) -PassThru -Wait
        if ($comboFlatProcess.ExitCode) { throw "Package neutral ComboBox checks failed: $comboFlatEvidence/result.log" }
        $editorEvidence = Join-Path $nativeRoot 'TestResults/visual/m2b-package-consumer'
        # Interactive native input needs a shown window; a hidden startup window
        # can report managed Visible=true while its child HWND cannot take focus.
        $editorProcess = Start-Process -FilePath $consumerExe -ArgumentList @('--validate-editors', ('"' + $editorEvidence + '"')) -PassThru -Wait
        if ($editorProcess.ExitCode) { throw "Package editor checks failed: $editorEvidence/result.log" }
        $splitEvidence = Join-Path $nativeRoot 'TestResults/visual/m2a-package-consumer'
        $splitProcess = Start-Process -FilePath $consumerExe -ArgumentList @('--validate-split', ('"' + $splitEvidence + '"')) -PassThru -Wait
        if ($splitProcess.ExitCode) { throw "Package split-button checks failed: $splitEvidence/result.log" }
        $flatEvidence = Join-Path $nativeRoot 'TestResults/visual/renderer-flat-package'
        $flatProcess = Start-Process -FilePath $consumerExe -ArgumentList @('--validate-split-flat', ('"' + $flatEvidence + '"')) -PassThru -Wait
        if ($flatProcess.ExitCode) { throw "Package flat-renderer checks failed: $flatEvidence/result.log" }
        dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --app $consumerExe
        if ($LASTEXITCODE) { throw 'Isolated package consumer UIA/input checks failed.' }
        dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --keyboard --app $consumerExe
        if ($LASTEXITCODE) { throw 'Isolated package consumer keyboard checks failed.' }
        dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --focus-cues TestResults/visual/focus-package-consumer --app $consumerExe
        if ($LASTEXITCODE) { throw 'Isolated package consumer focus appearance checks failed.' }
        dotnet run --project tests/RibbonKit.WinForms.NativeTests -c $Configuration --no-build -- --popup-dismissal --app $consumerExe
        if ($LASTEXITCODE) { throw 'Isolated package consumer popup dismissal checks failed.' }
        Write-Host "Package consumer evidence retained at $consumerRoot"
    }
} finally { Pop-Location }
