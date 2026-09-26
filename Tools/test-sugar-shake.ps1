$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskMonoRoot = 'D:\Unity\Hub\6000.5.3f1\Editor\Data\MonoBleedingEdge'
New-Item -ItemType Directory -Path "$taskRoot\Logs" -Force | Out-Null
& "$taskMonoRoot\bin\mono.exe" "$taskMonoRoot\lib\mono\4.5\mcs.exe" -out:"$taskRoot\Logs\SugarShakeTests.exe" "$taskRoot\Assets\CottonCircuit\Scripts\Core\SugarShake.cs" "$taskRoot\Tools\Tests\SugarShakeTests.cs"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& "$taskMonoRoot\bin\mono.exe" "$taskRoot\Logs\SugarShakeTests.exe" | Tee-Object -FilePath "$taskRoot\Logs\sugar-shake-tests.txt"
exit $LASTEXITCODE
