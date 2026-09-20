$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskMonoRoot = 'D:\Unity\Hub\6000.5.3f1\Editor\Data\MonoBleedingEdge'
New-Item -ItemType Directory -Path "$taskRoot\Logs" -Force | Out-Null
& "$taskMonoRoot\bin\mono.exe" "$taskMonoRoot\lib\mono\4.5\mcs.exe" -out:"$taskRoot\Logs\CoreTests.exe" "$taskRoot\Assets\CottonCircuit\Scripts\Core\Production.cs" "$taskRoot\Assets\CottonCircuit\Scripts\Core\Economy.cs" "$taskRoot\Tools\Tests\CoreTests.cs"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& "$taskMonoRoot\bin\mono.exe" "$taskRoot\Logs\CoreTests.exe" | Tee-Object -FilePath "$taskRoot\Logs\core-tests.txt"
exit $LASTEXITCODE
