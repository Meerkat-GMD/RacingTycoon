$ErrorActionPreference = 'Stop'
$taskMcpRoot = $PSScriptRoot
$taskMcpRuntime = Join-Path $taskMcpRoot 'runtime'
New-Item -ItemType Directory -Path $taskMcpRuntime -Force | Out-Null
$taskMcpExisting = Get-NetTCPConnection -LocalPort 9876 -State Listen -ErrorAction SilentlyContinue
if ($taskMcpExisting) {
    throw "Port 9876 is already listening; inspect it before starting another Blender session."
}
$taskMcpProcess = Start-Process -FilePath 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -ArgumentList @('--factory-startup', '--disable-autoexec', '--python', ('"' + (Join-Path $taskMcpRoot 'bootstrap.py') + '"')) -WindowStyle Hidden -RedirectStandardOutput (Join-Path $taskMcpRuntime 'blender-stdout.log') -RedirectStandardError (Join-Path $taskMcpRuntime 'blender-stderr.log') -PassThru
$taskMcpProcess.Id | Set-Content -LiteralPath (Join-Path $taskMcpRuntime 'blender.pid')
Write-Output "Started ephemeral Blender MCP process $($taskMcpProcess.Id)."
