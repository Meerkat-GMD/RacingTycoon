$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskMonoRoot = 'D:\Unity\Hub\6000.5.3f1\Editor\Data\MonoBleedingEdge'
New-Item -ItemType Directory -Path "$taskRoot\Logs" -Force | Out-Null
$taskSources = Get-ChildItem -LiteralPath "$taskRoot\Assets\CottonCircuit\Scripts\Core" -Filter '*.cs' | ForEach-Object { $_.FullName }
& "$taskMonoRoot\bin\mono.exe" "$taskMonoRoot\lib\mono\4.5\mcs.exe" -out:"$taskRoot\Logs\CoreTests.exe" @taskSources "$taskRoot\Tools\Tests\CoreTests.cs"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& "$taskMonoRoot\bin\mono.exe" "$taskRoot\Logs\CoreTests.exe" | Tee-Object -FilePath "$taskRoot\Logs\core-tests.txt"
$taskCoreResult = $LASTEXITCODE
& "$taskMonoRoot\bin\mono.exe" "$taskMonoRoot\lib\mono\4.5\mcs.exe" -out:"$taskRoot\Logs\DrivingTests.exe" @taskSources "$taskRoot\Tools\Tests\DrivingTests.cs"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& "$taskMonoRoot\bin\mono.exe" "$taskRoot\Logs\DrivingTests.exe" | Tee-Object -FilePath "$taskRoot\Logs\driving-tests.txt"
$taskDrivingResult = $LASTEXITCODE
& "$taskMonoRoot\bin\mono.exe" "$taskMonoRoot\lib\mono\4.5\mcs.exe" -out:"$taskRoot\Logs\OrderTests.exe" @taskSources "$taskRoot\Tools\Tests\OrderTests.cs"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& "$taskMonoRoot\bin\mono.exe" "$taskRoot\Logs\OrderTests.exe" | Tee-Object -FilePath "$taskRoot\Logs\order-tests.txt"
$taskOrderResult = $LASTEXITCODE
if ($taskCoreResult -ne 0) { exit $taskCoreResult }
if ($taskDrivingResult -ne 0) { exit $taskDrivingResult }
exit $taskOrderResult
