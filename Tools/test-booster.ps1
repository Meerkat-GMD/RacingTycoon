$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskMonoRoot = 'D:\Unity\Hub\6000.5.3f1\Editor\Data\MonoBleedingEdge'
New-Item -ItemType Directory -Path "$taskRoot\Logs" -Force | Out-Null
$taskSources = Get-ChildItem -LiteralPath "$taskRoot\Assets\CottonCircuit\Scripts\Core" -Filter '*.cs' | ForEach-Object { $_.FullName }
& "$taskMonoRoot\bin\mono.exe" "$taskMonoRoot\lib\mono\4.5\mcs.exe" -out:"$taskRoot\Logs\BoosterTests.exe" @taskSources "$taskRoot\Tools\Tests\BoosterTests.cs"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& "$taskMonoRoot\bin\mono.exe" "$taskRoot\Logs\BoosterTests.exe" | Tee-Object -FilePath "$taskRoot\Logs\booster-tests.txt"
exit $LASTEXITCODE
