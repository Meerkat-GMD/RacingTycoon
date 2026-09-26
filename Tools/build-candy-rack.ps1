param(
    [string]$UnityPath = 'D:\Unity\Hub\6000.5.3f1\Editor\Unity.exe',
    [string]$BuildFolder = 'Builds\CandyRack',
    [switch]$Release
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskOutput = [IO.Path]::GetFullPath((Join-Path $taskRoot $BuildFolder))
$taskConfiguration = if ($Release) { 'Release' } else { 'Development' }
$taskLog = Join-Path $taskRoot ('Logs\candy-rack-' + $taskConfiguration.ToLowerInvariant() + '-build.log')
New-Item -ItemType Directory -Path (Join-Path $taskRoot 'Logs') -Force | Out-Null
$taskArguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $taskRoot + '"'),
    '-executeMethod', 'CottonCircuit.Editor.CandyRackBuild.Build',
    '-cotton-build-output', ('"' + $taskOutput + '"'), '-quit', '-logFile', ('"' + $taskLog + '"'))
if ($Release) { $taskArguments += '-cotton-release' }
$taskBuild = Start-Process -FilePath $UnityPath -ArgumentList $taskArguments -WindowStyle Hidden -PassThru
$taskBuild.WaitForExit()
if ($taskBuild.ExitCode -ne 0) { Get-Content -LiteralPath $taskLog -Tail 60; exit $taskBuild.ExitCode }
@{ configuration = $taskConfiguration; unity = '6000.5.3f1'; builtUtc = [DateTime]::UtcNow.ToString('o'); executable = 'CottonCircuit.exe' } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskOutput 'build-info.json') -Encoding utf8
Write-Output "Build ready: $taskOutput\CottonCircuit.exe"
