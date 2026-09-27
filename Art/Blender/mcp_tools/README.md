# Task-local Blender MCP

This directory runs the actual [MCP for Blender](https://github.com/ahujasid/mcp-for-blender) server over stdio JSON-RPC, connected to its official Blender socket add-on. It does not register an MCP server in Codex or install a Blender add-on globally.

## Provenance

- Official PyPI server: `mcp-for-blender==2.1.0` (Python MCP SDK `mcp==1.30.0`).
- `addon.py`: upstream commit `8ce9f8d45be9370db420c9f6df521277f69819ab`.
- Add-on SHA256: `28c4be2e4acc40cfb96dd947fe30e99ada884317f4fe3fedb5f5611ec4a0bf30`.
- Downloaded add-on exactly matches the copy bundled in the installed PyPI package.
- Upstream MIT license is included as `UPSTREAM_LICENSE`.

## Start and use (PowerShell, repository root)

```powershell
# Dependencies are installed only under this task directory.
uv venv --python 'C:/Users/gmd13/AppData/Local/Programs/Python/Python313/python.exe' 'Art/Blender/mcp_tools/.venv'
uv pip install --python 'Art/Blender/mcp_tools/.venv/Scripts/python.exe' 'mcp-for-blender==2.1.0'

# Once per session; refuses to replace an existing port 9876 listener.
& 'Art/Blender/mcp_tools/start.ps1'

# Replace the prompt with the user's actual request.
& 'Art/Blender/mcp_tools/.venv/Scripts/python.exe' 'Art/Blender/mcp_tools/client.py' get_scene_info --prompt 'User request'
& 'Art/Blender/mcp_tools/.venv/Scripts/python.exe' 'Art/Blender/mcp_tools/client.py' execute_blender_code --code-file 'D:/path/to/script.py' --prompt 'User request' --output 'D:/path/to/result.json'
```

The client creates one official MCP server subprocess per invocation and calls `initialize`, then `tools/list` or `tools/call` through the Python MCP SDK. It prints the actual MCP result and can also save it with `--output`. Additional tool parameters can be passed as JSON with `--arguments`.

The Blender process is launched hidden using `--factory-startup --disable-autoexec`, with the local `bootstrap.py`. This retains the normal GUI event loop: the official add-on cannot execute queued commands in `blender -b` mode. It uses `127.0.0.1:9876`, and makes no changes to startup files or saved preferences. Keep calls sequential and split renders into calls under the official server's 180-second socket timeout. The default client timeout is 240 seconds.

Telemetry is disabled by environment variable for the MCP subprocess. Its AppData and configuration paths are redirected into `runtime/`, and the ephemeral add-on reports telemetry consent false. Runtime logs, PID, MCP result envelopes and the virtual environment are ignored by Git.

The bootstrap initially contains Blender's factory default Camera, Cube and Light. Models or render settings are created only by subsequent `execute_blender_code` calls.

## Stop

Once all scene work is saved and no task uses the session, check the command line and process ID recorded in `runtime/blender.pid`, then stop that specific Blender process. The short-lived MCP stdio server processes exit when each client call completes.
