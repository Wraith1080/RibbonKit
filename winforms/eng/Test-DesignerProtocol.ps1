param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [string]$ProtocolAssembly,
    [string]$VisualStudioPath
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$nativeRoot = Split-Path $PSScriptRoot -Parent
if (-not $ProtocolAssembly) {
    $ProtocolAssembly = Join-Path $nativeRoot "src/RibbonKit.WinForms.Design.Protocol/bin/$Configuration/net472/RibbonKit.WinForms.Design.Protocol.dll"
}
$ProtocolAssembly = (Resolve-Path -LiteralPath $ProtocolAssembly).Path
if (-not $VisualStudioPath) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
    $VisualStudioPath = & $vswhere -latest -products '*' -property installationPath
    if (-not $VisualStudioPath) { throw 'An installed Visual Studio WinForms designer is required for this protocol registration check.' }
}
# Exercise the IDE client's actual .NET Framework registry in a fresh process.
# SDK reference assemblies alone cannot reproduce its startup registration.
if ($PSVersionTable.PSEdition -eq 'Core') {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -Configuration $Configuration -ProtocolAssembly $ProtocolAssembly -VisualStudioPath $VisualStudioPath
    if ($LASTEXITCODE) { throw 'Designer protocol registration check failed.' }
    return
}
$sdkRoot = Join-Path $VisualStudioPath 'Common7/IDE/CommonExtensions/Microsoft/Windows.Forms'
$fxRoot = Join-Path $sdkRoot 'FxDesignToolsServer/Common'
$protocolBin = Split-Path $ProtocolAssembly -Parent
$probeDirectories = @($sdkRoot, $fxRoot, $protocolBin)
$loaded = @{}
# The IDE supplies its own runtime/binding policy. Use its matching composition
# implementation rather than a consumer's SDK 1.6 reference dependencies.
Get-ChildItem -LiteralPath $fxRoot -Filter 'System.Composition.*.dll' | ForEach-Object {
    $assembly = [Reflection.Assembly]::LoadFrom($_.FullName)
    $loaded[$assembly.GetName().Name] = $assembly
}
$resolver = [ResolveEventHandler]{
    param($sender, $eventArgs)
    $name = [Reflection.AssemblyName]::new($eventArgs.Name).Name
    if ($loaded.ContainsKey($name)) { return $loaded[$name] }
    foreach ($directory in $probeDirectories) {
        $candidate = Join-Path $directory ($name + '.dll')
        if (Test-Path -LiteralPath $candidate) {
            $assembly = [Reflection.Assembly]::LoadFrom($candidate)
            $loaded[$name] = $assembly
            return $assembly
        }
    }
    return $null
}.GetNewClosure()
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
$composition = $null
try {
    $sdk = [Reflection.Assembly]::LoadFrom((Join-Path $sdkRoot 'Microsoft.DotNet.DesignTools.Protocol.dll'))
    $loaded[$sdk.GetName().Name] = $sdk
    $extension = [Reflection.Assembly]::LoadFrom($ProtocolAssembly)
    $compositionConfiguration = [System.Composition.Hosting.ContainerConfiguration]::new().WithAssembly($extension)
    $composition = $compositionConfiguration.CreateContainer()
    $registryType = $sdk.GetType('Microsoft.DotNet.DesignTools.Protocol.ProtocolDefinition', $true)
    $create = $registryType.GetMethod('Create', [Reflection.BindingFlags]'Static,Public,NonPublic')
    $registry = $create.Invoke($null, [object[]]@($composition))
    foreach ($name in @('RibbonKit.EditRibbon', 'RibbonKit.EditBackstage', 'RibbonKit.EditApplicationMenu')) {
        $endpoint = $registry.GetEndpoint($name)
        if (-not [object]::ReferenceEquals($endpoint, $registry.GetEndpoint($endpoint.RequestType)) -or
            -not [object]::ReferenceEquals($endpoint, $registry.GetEndpoint($endpoint.ResponseType))) {
            throw "Endpoint cannot be resolved consistently by name/request/response type: $name"
        }
        Write-Output ('PASS: {0} resolves by name, {1} and {2}.' -f $name, $endpoint.RequestType.Name, $endpoint.ResponseType.Name)
    }
    Write-Output ('PASS: actual VS {0} client ProtocolDefinition.Create with composed extension exports.' -f $sdk.GetName().Version)
}
catch {
    $failure = $_.Exception.GetBaseException()
    Write-Error ('FAIL: designer protocol composition/registration: ' + $failure.Message + [Environment]::NewLine + $failure.StackTrace) -ErrorAction Continue
    exit 1
}
finally {
    if ($null -ne $composition) { $composition.Dispose() }
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)
}
