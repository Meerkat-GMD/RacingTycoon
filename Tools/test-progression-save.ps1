$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskMonoRoot = 'D:\Unity\Hub\6000.5.3f1\Editor\Data\MonoBleedingEdge'
New-Item -ItemType Directory -Path "$taskRoot\Logs" -Force | Out-Null
$taskSources = Get-ChildItem -LiteralPath "$taskRoot\Assets\CottonCircuit\Scripts\Core" -Filter '*.cs' | ForEach-Object { $_.FullName }
& "$taskMonoRoot\bin\mono.exe" "$taskMonoRoot\lib\mono\4.5\mcs.exe" -r:"$taskMonoRoot\lib\mono\4.5\Facades\netstandard.dll" -r:"$taskMonoRoot\lib\mono\4.5\System.Web.Extensions.dll" -out:"$taskRoot\Logs\ProgressionSaveTests.exe" @taskSources "$taskRoot\Assets\CottonCircuit\Scripts\SaveStore.cs" "$taskRoot\Tools\Tests\ProgressionSaveTests.cs"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& "$taskMonoRoot\bin\mono.exe" "$taskRoot\Logs\ProgressionSaveTests.exe" | Tee-Object -FilePath "$taskRoot\Logs\progression-save-tests.txt"
exit $LASTEXITCODE
