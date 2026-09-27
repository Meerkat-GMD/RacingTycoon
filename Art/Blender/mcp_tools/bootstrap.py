"""Load the official MCP addon only for this factory-startup Blender session."""
from pathlib import Path
import importlib.util
import sys

import bpy

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("task_blender_mcp", ROOT / "addon.py")
addon = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = addon
spec.loader.exec_module(addon)
addon.register()
bpy.context.scene.blendermcp_auto_start_server = False
server = addon.BlenderMCPServer(host="127.0.0.1", port=9876)
bpy.types.blendermcp_server = server
server.start()
bpy.context.scene.blendermcp_server_running = server.running
print("TASK_BLENDER_MCP_READY", bpy.app.version_string, server.running, flush=True)
