param(
    [string]$BuildFolder = 'Builds\UIToolkit',
    [string]$OutputFolder = 'Logs\TutorialSmoke',
    [ValidateSet('full', 'skip', 'resume', 'hint', 'drive-control', 'drive-view', 'drive-legacy', 'worker-driving')][string]$Case = 'full',
    [int]$Width = 1600,
    [int]$Height = 900
)
$ErrorActionPreference = 'Stop'
$toolkitCase = if ($Case -eq 'drive-legacy') { 'legacy' } else { 'full' }
Write-Warning "The old tutorial '$Case' suite is retired. Running the current UITK '$toolkitCase' flow; it does not reproduce the old scenario's full coverage or check count. Prefer Tools/verify-uitk.ps1."
& (Join-Path $PSScriptRoot 'verify-uitk.ps1') -BuildFolder $BuildFolder -OutputFolder $OutputFolder -Case $toolkitCase -Width $Width -Height $Height
exit $LASTEXITCODE
