"""CLI for the official MCP server using its real stdio JSON-RPC transport.

Examples:
  .venv/Scripts/python.exe client.py get_scene_info --prompt "user wording"
  .venv/Scripts/python.exe client.py execute_blender_code --code-file script.py --prompt "user wording"
  .venv/Scripts/python.exe client.py list_tools
"""
from __future__ import annotations

import argparse
import asyncio
from datetime import timedelta
import json
import os
from pathlib import Path
import sys

from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

ROOT = Path(__file__).resolve().parent


async def run(args):
    runtime = ROOT / "runtime"
    runtime.mkdir(exist_ok=True)
    env = dict(os.environ)
    env.update({
        "BLENDER_HOST": "127.0.0.1",
        "BLENDER_PORT": "9876",
        "BLENDER_MCP_DISABLE_TELEMETRY": "1",
        "APPDATA": str(runtime / "appdata"),
        "XDG_CONFIG_HOME": str(runtime / "config"),
        "PYTHONIOENCODING": "utf-8",
    })
    parameters = StdioServerParameters(
        command=str(ROOT / ".venv" / "Scripts" / "mcp-for-blender.exe"),
        args=[],
        env=env,
        cwd=str(ROOT),
    )
    with (runtime / "server-stderr.log").open("a", encoding="utf-8") as log:
        async with stdio_client(parameters, errlog=log) as (reader, writer):
            async with ClientSession(reader, writer, read_timeout_seconds=timedelta(seconds=args.timeout)) as session:
                initialization = await session.initialize()
                if args.tool == "list_tools":
                    result = await session.list_tools()
                else:
                    arguments = json.loads(args.arguments) if args.arguments else {}
                    if args.prompt is not None:
                        arguments["user_prompt"] = args.prompt
                    if args.code_file:
                        arguments["code"] = Path(args.code_file).read_text(encoding="utf-8-sig")
                    result = await session.call_tool(args.tool, arguments=arguments)
                payload = {
                    "transport": "MCP stdio JSON-RPC",
                    "server": initialization.serverInfo.model_dump(mode="json"),
                    "tool": args.tool,
                    "result": result.model_dump(mode="json", exclude_none=True),
                }
                serialized = json.dumps(payload, ensure_ascii=False, indent=2)
                if args.output:
                    Path(args.output).write_text(serialized + "\n", encoding="utf-8")
                print(serialized)
                if getattr(result, "isError", False):
                    return 1
                # Upstream reports Python failures as tool text with isError=false.
                # Preserve its exact result while giving shell callers a failure code.
                for item in getattr(result, "content", []):
                    message = getattr(item, "text", "")
                    if message.startswith(("Error executing code:", "Rejected by safe mode")):
                        return 1
                return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("tool")
    parser.add_argument("--code-file")
    parser.add_argument("--prompt", help="The user's verbatim request; required by get_scene_info")
    parser.add_argument("--arguments", help="Additional MCP tool arguments as JSON")
    parser.add_argument("--output", help="Write the exact MCP result envelope to this file")
    parser.add_argument("--timeout", type=float, default=240)
    args = parser.parse_args()
    return asyncio.run(run(args))


if __name__ == "__main__":
    sys.exit(main())
