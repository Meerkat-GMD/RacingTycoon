"""Read-only probe executed through MCP execute_blender_code."""
import bpy
import json

print(json.dumps({
    "blender_version": bpy.app.version_string,
    "filepath": bpy.data.filepath,
    "background": bpy.app.background,
    "scene": bpy.context.scene.name,
    "objects": [obj.name for obj in bpy.context.scene.objects],
}, sort_keys=True))
