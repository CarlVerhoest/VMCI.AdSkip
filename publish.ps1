# Publishes AdSkip to .\publish:
#  - AdSkipTray.exe: tray app, self-contained (no .NET install needed), the one to copy to other machines
#  - AdSkip.exe:     console app, framework-dependent (needs the .NET 10 Desktop Runtime), handy for testing
#  - appsettings.json, shared by both
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$output = Join-Path $root 'publish'

dotnet publish (Join-Path $root 'VMCI.AdSkip\VMCI.AdSkip.csproj') `
    -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true -p:DebugType=None `
    -o $output
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet publish (Join-Path $root 'VMCI.AdSkip.Tray\VMCI.AdSkip.Tray.csproj') `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -o $output
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Published to $output"
Get-ChildItem $output | Format-Table Name, @{ n = 'Size (MB)'; e = { [math]::Round($_.Length / 1MB, 1) } }
