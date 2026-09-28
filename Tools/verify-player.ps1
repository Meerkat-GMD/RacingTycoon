param([string]$OutputFolder = 'Logs\Smoke', [string]$BuildFolder = 'Builds\UIToolkit', [switch]$ShopShift)
$ErrorActionPreference = 'Stop'
$toolkitCase = if ($ShopShift) { 'full' } else { 'legacy' }
Write-Warning "The old uGUI player suites are retired. Running the current UITK '$toolkitCase' flow; it does not reproduce the old scenario's full coverage or check count. Prefer Tools/verify-uitk.ps1 and retain the standalone rule tests."
& (Join-Path $PSScriptRoot 'verify-uitk.ps1') -BuildFolder $BuildFolder -OutputFolder $OutputFolder -Case $toolkitCase
exit $LASTEXITCODE
