param(
    [string]$BuildFolder = 'Builds\UIToolkit',
    [string]$OutputFolder = 'Logs\TitleScreenSmoke',
    [ValidateSet('fresh', 'saved', 'reset', 'corrupt')][string]$Case = 'fresh',
    [int]$Width = 1600,
    [int]$Height = 900,
    [ValidateRange(-1, 5)][int]$IntroSkipAt = -1
)
$ErrorActionPreference = 'Stop'
if ($Case -eq 'corrupt' -or $IntroSkipAt -ge 0) {
    throw 'The old uGUI corrupt/IntroSkipAt scenarios are retired and are not covered by the UITK flow test. Use Tools/verify-uitk.ps1 -Case full for the current title/story/tutorial flow; use the standalone save tests for save rules.'
}
Write-Warning "The old title '$Case' suite is retired. Running the current UITK full flow, which includes fresh title, resume, new-game confirmation and story skip; its scope and check count differ. Prefer Tools/verify-uitk.ps1."
& (Join-Path $PSScriptRoot 'verify-uitk.ps1') -BuildFolder $BuildFolder -OutputFolder $OutputFolder -Case full -Width $Width -Height $Height
exit $LASTEXITCODE
