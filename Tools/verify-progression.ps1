param([string]$OutputFolder = 'Logs\ProgressionSmoke', [string]$BuildFolder = 'Builds\UIToolkit')
$ErrorActionPreference = 'Stop'
Write-Warning 'The old uGUI progression suite is retired. Running the current UITK full flow with preparation, purchases and worker interactions; its scope and check count differ. Prefer Tools/verify-uitk.ps1 and retain the standalone test-progression*.ps1 rule tests.'
& (Join-Path $PSScriptRoot 'verify-uitk.ps1') -BuildFolder $BuildFolder -OutputFolder $OutputFolder -Case full
exit $LASTEXITCODE
