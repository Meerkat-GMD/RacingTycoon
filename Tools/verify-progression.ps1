param([string]$OutputFolder = 'Logs\ProgressionSmoke', [string]$BuildFolder = 'Builds\Windows')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskOutput = [IO.Path]::GetFullPath((Join-Path $taskRoot $OutputFolder))
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
$taskPlayer = Join-Path $taskRoot "$BuildFolder\CottonCircuit.exe"
$taskProcess = Start-Process -FilePath $taskPlayer -ArgumentList @('--progression-smoke', ('"--smoke-dir=' + $taskOutput + '"'), '-screen-fullscreen', '0', '-logFile', ('"' + $taskOutput + '\player.log"')) -WorkingDirectory $taskRoot -WindowStyle Hidden -PassThru
if (-not $taskProcess.WaitForExit(180000)) { Stop-Process -Id $taskProcess.Id; throw 'Progression verification exceeded three minutes. See player.log.' }
Get-Content -LiteralPath (Join-Path $taskOutput 'result.txt')
exit $taskProcess.ExitCode
