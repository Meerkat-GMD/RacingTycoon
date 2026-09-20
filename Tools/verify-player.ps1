param([string]$OutputFolder = 'Logs\Smoke')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskOutput = [IO.Path]::GetFullPath((Join-Path $taskRoot $OutputFolder))
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
$taskPlayer = Join-Path $taskRoot 'Builds\Windows\CottonCircuit.exe'
if (-not (Test-Path -LiteralPath $taskPlayer)) { throw 'Build the development player first with Tools/build.ps1.' }
$taskReceiptPath = Join-Path $taskRoot 'Builds\Windows\build-info.json'
if ((Test-Path -LiteralPath $taskReceiptPath) -and (Get-Content -Raw -LiteralPath $taskReceiptPath | ConvertFrom-Json).configuration -eq 'Release') { throw 'The current player is a release build. Run Tools/build.ps1 without -Release, then verify again.' }
$taskProcess = Start-Process -FilePath $taskPlayer -ArgumentList @('--smoke-test', ('"--smoke-dir=' + $taskOutput + '"'), '-screen-fullscreen', '0', '-logFile', ('"' + $taskOutput + '\player.log"')) -WorkingDirectory $taskRoot -WindowStyle Hidden -PassThru
if (-not $taskProcess.WaitForExit(120000)) { Stop-Process -Id $taskProcess.Id; throw 'The verification player exceeded its two-minute time limit. See player.log.' }
Get-Content -LiteralPath (Join-Path $taskOutput 'result.txt')
exit $taskProcess.ExitCode
