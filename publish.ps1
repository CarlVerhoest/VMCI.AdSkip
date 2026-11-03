# Publishes AdSkip.exe (framework-dependent, single file) with its appsettings.json to .\publish
# Requires the .NET 10 Desktop Runtime on the machine that runs it.
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$output = Join-Path $root 'publish'

dotnet publish (Join-Path $root 'VMCI.AdSkip\VMCI.AdSkip.csproj') `
    -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true -p:DebugType=None `
    -o $output
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Published to $output\AdSkip.exe"
