$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
New-Item -ItemType Directory -Path "$taskRoot/Logs" -Force | Out-Null
$taskMono = 'D:\Unity\Hub\6000.5.3f1\Editor\Data\MonoBleedingEdge'
$taskSource = Get-ChildItem "$taskRoot/Assets/CottonCircuit/Scripts/Core" -Filter '*.cs' | ForEach-Object FullName
& "$taskMono/bin/mono.exe" "$taskMono/lib/mono/4.5/mcs.exe" -out:"$taskRoot/Logs/CollectionTests.exe" @taskSource "$taskRoot/Tools/Tests/CollectionTests.cs"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& "$taskMono/bin/mono.exe" "$taskRoot/Logs/CollectionTests.exe" | Tee-Object -FilePath "$taskRoot/Logs/collection-tests.txt"
exit $LASTEXITCODE
