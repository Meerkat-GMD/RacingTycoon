param([string]$UnityPath = 'D:\Unity\Hub\6000.5.3f1\Editor\Unity.exe', [switch]$Release, [string]$BuildFolder = 'Builds\Windows')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskBuildFolder = [IO.Path]::GetFullPath((Join-Path $taskRoot $BuildFolder))
if (-not (Test-Path -LiteralPath $UnityPath)) { throw "Unity 6000.5.3f1 executable not found: $UnityPath" }
New-Item -ItemType Directory -Path "$taskRoot\Logs" -Force | Out-Null
$taskMethod = if ($Release) { 'CottonCircuit.Editor.ProjectBuilder.BuildRelease' } else { 'CottonCircuit.Editor.ProjectBuilder.VerifyAndBuild' }
$taskBuild = Start-Process -FilePath $UnityPath -ArgumentList @('-batchmode', '-nographics', '-projectPath', ('"' + $taskRoot + '"'), '-executeMethod', $taskMethod, '-cotton-build-output', ('"' + $taskBuildFolder + '"'), '-quit', '-logFile', ('"' + $taskRoot + '\Logs\build.log"')) -WindowStyle Hidden -Wait -PassThru
if ($taskBuild.ExitCode -ne 0) { Get-Content -LiteralPath "$taskRoot\Logs\build.log" -Tail 60; exit $taskBuild.ExitCode }
$taskConfiguration = if ($Release) { 'Release' } else { 'Development' }
@{ configuration = $taskConfiguration; unity = '6000.5.3f1'; builtUtc = [DateTime]::UtcNow.ToString('o'); executable = 'CottonCircuit.exe' } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskBuildFolder 'build-info.json') -Encoding utf8
Write-Output "Build ready: $taskBuildFolder\CottonCircuit.exe"
