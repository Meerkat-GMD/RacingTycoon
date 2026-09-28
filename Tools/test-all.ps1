# Runs every Tools/test-*.ps1 core test script and fails if any of them fails.
$ErrorActionPreference = 'Continue'
$taskFailed = @()
foreach ($script in Get-ChildItem -LiteralPath $PSScriptRoot -Filter 'test-*.ps1' | Where-Object Name -ne 'test-all.ps1') {
    & powershell -ExecutionPolicy Bypass -File $script.FullName *> $null
    if ($LASTEXITCODE -ne 0) { $taskFailed += $script.Name; Write-Output "FAIL $($script.Name)" } else { Write-Output "PASS $($script.Name)" }
}
if ($taskFailed.Count -gt 0) { Write-Output ("Failed: " + ($taskFailed -join ', ')); exit 1 }
Write-Output 'All core test scripts passed.'
