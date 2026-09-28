param(
    [string]$BuildFolder = 'Builds\UIToolkit',
    [string]$OutputFolder = 'Logs\UIToolkitSmoke',
    [ValidateSet('full', 'legacy', 'rack', 'hud', 'music', 'settings')][string]$Case = 'full',
    [int]$Width = 1600,
    [int]$Height = 900,
    [ValidateSet('ko', 'en')][string]$Language = 'ko'
)
$ErrorActionPreference = 'Stop'
$toolkitRoot = Split-Path -Parent $PSScriptRoot
$toolkitOutput = [IO.Path]::GetFullPath((Join-Path $toolkitRoot $OutputFolder))
$toolkitPlayer = Join-Path $toolkitRoot "$BuildFolder\CottonCircuit.exe"
if (-not (Test-Path -LiteralPath $toolkitPlayer)) { throw 'Build a development player with Tools/build.ps1 first.' }
$toolkitReceipt = Join-Path $toolkitRoot "$BuildFolder\build-info.json"
if ((Test-Path -LiteralPath $toolkitReceipt) -and (Get-Content -Raw -LiteralPath $toolkitReceipt | ConvertFrom-Json).configuration -eq 'Release') {
    throw 'UI Toolkit smoke checks require a development player.'
}
New-Item -ItemType Directory -Path $toolkitOutput -Force | Out-Null
$toolkitResult = Join-Path $toolkitOutput 'result.txt'
if (Test-Path -LiteralPath $toolkitResult) { Remove-Item -LiteralPath $toolkitResult }
$toolkitArguments = @(
    '--uitk-smoke', "--uitk-case=$Case", "--uitk-width=$Width", "--uitk-height=$Height", "--language=$Language",
    ('"--smoke-dir=' + $toolkitOutput + '"'), '-screen-fullscreen', '0',
    '-screen-width', ($Width + 16).ToString(), '-screen-height', $Height.ToString(),
    '-logFile', ('"' + $toolkitOutput + '\player.log"')
)
$toolkitProcess = Start-Process -FilePath $toolkitPlayer -ArgumentList $toolkitArguments -WorkingDirectory $toolkitRoot -WindowStyle Hidden -PassThru
if (-not $toolkitProcess.WaitForExit(180000)) {
    Stop-Process -Id $toolkitProcess.Id
    throw 'UI Toolkit check exceeded three minutes. See player.log.'
}
if (-not (Test-Path -LiteralPath $toolkitResult)) { throw 'UI Toolkit check did not produce a result. See player.log.' }
Get-Content -LiteralPath $toolkitResult
exit $toolkitProcess.ExitCode
