# MCP Integration Design for Pixel Art Generation

## Overview

This document outlines the design for integrating KBTV's pixel art generation tool with the Model Context Protocol (MCP), allowing opencode to call the generation tool directly.

## Current Status

- ✅ Authoritative prompt rules: `docs/art/PIXELLAB_PROMPT_RULES.md` (replaces the deleted `docs/art/PIXEL_ART_PROMPTS.md`)
- ✅ Batch generator script: `Tools/ArtGeneration/generate_pixel_art.py`
- ✅ **PixelLab MCP connected** - User has external PixelLab MCP server configured
- 📖 Usage guide: See `PIXELLAB_MCP_GUIDE.md`

The PixelLab MCP provides direct AI image generation from opencode. The user has connected their own PixelLab MCP server, so no custom implementation is needed.

## Proposed Architecture

### Option 1: Direct Python MCP Server (Recommended)

Create a lightweight MCP server that wraps the existing generator:

```
┌─────────────┐
│  opencode  │
│  (MCP)      │
└──────┬──────┘
       │ MCP protocol (stdio/JSON-RPC)
       ▼
┌─────────────────────┐
│  PixelArt MCP       │
│  Server             │
│  (Python)           │
└─────────┬───────────┘
          │ calls
          ▼
┌─────────────────────┐
│  generate_pixel_    │
│  art.py (existing)  │
└─────────┬───────────┘
          │ API calls
          ▼
┌─────────────────────┐
│  Leonardo.ai /      │
│  DALL-E 3           │
└─────────────────────┘
```

**Implementation:**
- Add MCP server as new mode in `generate_pixel_art.py` (or separate file)
- Exposes tools: `generate_asset`, `generate_category`, `list_assets`
- Communicates via stdio (standard MCP pattern)
- Can run as standalone process that opencode connects to

**Implementation effort:** ~300 lines of Python

### Option 2: Shell MCP Bridge

Use existing MCP shell capabilities to call the script directly:

```
opencode -> MCP shell tool -> python generate_pixel_art.py [args]
```

**Pros:**
- Zero additional code
- Uses existing script
- Simple JSON output parsing

**Cons:**
- No streaming updates
- Limited error handling
- No stateful session

**Implementation:** Configure MCP client to expose the script as a tool.

### Option 3: HTTP MCP Server

Run a local HTTP server that opencode can call via webhooks:

```
opencode -> HTTP POST -> localhost:PORT/generate
```

**Pros:**
- Browser-based management possible
- Easy to debug (curl, Postman)
- Can add web UI for monitoring

**Cons:**
- Requires server process management
- More boilerplate

**Implementation effort:** ~200 lines (Flask/FastAPI)

---

## Recommended: Option 1 (Direct Python MCP Server)

### Tool Definitions

#### Tool 1: `generate_pixel_art`

Generate one or more pixel art assets.

**Input:**
```json
{
  "category": "tiles",
  "subcategory": "floor",
  "assets": ["floor_tile_clean", "floor_tile_wear"],
  "variations": 3,
  "service": "leonardo",
  "dry_run": false
}
```

**Output:**
```json
{
  "success": true,
  "results": [
    {
      "name": "floor_tile_clean",
      "filepath": "assets/tiles/floor/floor_tile_clean.png",
      "success": true,
      "error": null,
      "generation_time": 45.2
    }
  ],
  "summary": {
    "total": 2,
    "success": 2,
    "failed": 0
  }
}
```

#### Tool 2: `list_available_assets`

List all assets that can be generated.

**Input:**
```json
{
  "category": "characters",
  "subcategory": "vern_portrait"
}
```

**Output:**
```json
{
  "assets": [
    {
      "name": "vern_portrait_neutral",
      "dimensions": [64, 64],
      "prompt": "radio host portrait..."
    },
    ...
  ]
}
```

#### Tool 3: `get_generation_status`

Check status of recent generations (if async mode).

**Input:**
```json
{
  "job_id": "abc123"
}
```

**Output:**
```json
{
  "job_id": "abc123",
  "status": "completed",  // or "running", "failed"
  "progress": 100,
  "results": [...]
}
```

### MCP Server Implementation Structure

```python
# Tools/ArtGeneration/pixel_art_mcp_server.py

import asyncio
from mcp import Server
from mcp.types import Tool, TextContent, ImageContent
from generate_pixel_art import PixelArtGenerator, LeonardoService, AssetSpec

class PixelArtMCPServer:
    def __init__(self):
        self.server = Server("pixel-art-generator")
        self.generator = None

    async def handle_generate(self, arguments: dict) -> list[TextContent | ImageContent]:
        category = arguments["category"]
        subcategory = arguments.get("subcategory")
        assets = arguments.get("assets", [])
        service_name = arguments.get("service", "leonardo")

        # Configure service
        api_key = os.environ.get(f"{service_name.upper()}_API_KEY", "")
        service = LeonardoService(api_key) if service_name == "leonardo" else DalleService(api_key)

        generator = PixelArtGenerator(service, dry_run=False)

        # Generate
        if assets:
            results = generator.generate_category(category, subcategory, specific_assets=assets)
        else:
            results = generator.generate_category(category, subcategory)

        # Format response
        return [TextContent(type="text", text=json.dumps({
            "success": True,
            "results": [
                {"name": r.asset_name, "filepath": str(r.filepath), "success": r.success}
                for r in results
            ],
            "summary": {
                "total": len(results),
                "success": sum(1 for r in results if r.success),
                "failed": sum(1 for r in results if not r.success)
            }
        }, indent=2))]

    async def handle_list_assets(self, arguments: dict) -> list[TextContent]:
        category = arguments["category"]
        subcategory = arguments.get("subcategory")

        parser = PromptParser(PROMPTS_FILE)
        specs = parser.parse_asset_prompts(category, subcategory)

        return [TextContent(type="text", text=json.dumps({
            "assets": [
                {"name": s.name, "dimensions": [s.width, s.height], "prompt": s.prompt}
                for s in specs
            ]
        }, indent=2))]

    async def run(self):
        # Set up MCP server handlers
        @self.server.list_tools()
        async def list_tools() -> list[Tool]:
            return [
                Tool(
                    name="generate_pixel_art",
                    description="Generate pixel art assets using AI",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "category": {"type": "string", "description": "Asset category"},
                            "subcategory": {"type": "string", "description": "Optional subcategory"},
                            "assets": {"type": "array", "items": {"type": "string"}},
                            "variations": {"type": "number"},
                            "service": {"type": "string", "enum": ["leonardo", "dalle"]},
                            "dry_run": {"type": "boolean"}
                        },
                        "required": ["category"]
                    }
                ),
                Tool(
                    name="list_available_assets",
                    description="List assets available for generation",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "category": {"type": "string"},
                            "subcategory": {"type": "string"}
                        },
                        "required": ["category"]
                    }
                )
            ]

        @self.server.call_tool()
        async def call_tool(name: str, arguments: dict) -> list[TextContent | ImageContent]:
            if name == "generate_pixel_art":
                return await self.handle_generate(arguments)
            elif name == "list_available_assets":
                return await self.handle_list_assets(arguments)
            else:
                raise ValueError(f"Unknown tool: {name}")

        # Run with stdio transport
        async with asyncio.stdio_client() as (read_stream, write_stream):
            await self.server.run(read_stream, write_stream, self.server.create_initialization_options())

if __name__ == "__main__":
    server = PixelArtMCPServer()
    asyncio.run(server.run())
```

### MCP Configuration

Add to opencode's MCP config (e.g., `~/.config/opencode/mcp.json`):

```json
{
  "mcpServers": {
    "pixel-art": {
      "command": "python",
      "args": ["D:/Dev/Games/kbtv/Tools/ArtGeneration/pixel_art_mcp_server.py"],
      "env": {
        "LEONARDO_API_KEY": "your_key_here"
      }
    }
  }
}
```

### Usage in opencode

Once connected, you can use:

```
Use MCP tool: pixel-art.generate_pixel_art
{
  "category": "tiles",
  "subcategory": "floor"
}
```

The results will appear in your tool response, and files will be created in `assets/`.

---

## Implementation Steps

### Phase 1: Already Complete
- [x] Prompt rules (PIXELLAB_PROMPT_RULES.md, replaces PIXEL_ART_PROMPTS.md)
- [x] Batch generator (generate_pixel_art.py)
- [x] CLI interface
- [x] Leonardo and DALL-E integrations

### Phase 2: MCP Wrapper (2-3 hours)
**To be implemented:**
- [ ] Install MCP Python SDK: `pip install mcp`
- [ ] Create `pixel_art_mcp_server.py` (above skeleton)
- [ ] Add tool definitions
- [ ] Implement stdio transport
- [ ] Error handling and validation
- [ ] Add configuration examples in README

### Phase 3: Testing & Polish (1 hour)
- [ ] Test MCP server connection
- [ ] Verify tool calls from opencode
- [ ] Add progress notifications (streaming)
- [ ] Document MCP usage in README

### Phase 4: Optional - Async Operations
- [ ] Add job queues for batch operations
- [ ] Implement `get_generation_status` tool
- [ ] Support cancellation
- [ ] Add generation history

---

## Alternative: Simple Shell MCP Bridge

If MCP Python SDK is too heavy, use shell tool (simpler):

```json
{
  "tools": [
    {
      "name": "generate_pixel_art",
      "description": "Generate pixel art assets",
      "command": "python",
      "args": [
        "D:/Dev/Games/kbtv/Tools/ArtGeneration/generate_pixel_art.py",
        "--category",
        "{category}"
      ],
      "inputs": {
        "category": {"type": "string", "required": true},
        "subcategory": {"type": "string"},
        "service": {"type": "string", "default": "leonardo"}
      }
    }
  ]
}
```

This runs the existing CLI tool directly. opencode would need to parse the stdout.

---

## Decision Needed

**Which approach do you prefer?**

1. **Full MCP server** (Option 1) - More robust, proper tool interface, streaming updates
2. **Shell wrapper** (Option 2) - Quick and dirty, uses existing script, minimal code
3. **HTTP server** (Option 3) - Separate process, easier debugging, web UI possible

Recommendation: **Option 1** for long-term maintainability and proper MCP integration.

If you want me to implement Option 1 now, I'll:
1. Install MCP dependencies
2. Create `pixel_art_mcp_server.py`
3. Update `requirements.txt`
4. Add MCP config example
5. Test end-to-end

Approach: **Full Python MCP server** with streaming progress and proper error messages.

---

## Questions

1. Do you want async operation support (background jobs)?
2. Should we add image preview support (return base64 of generated image)?
3. Should the MCP server read API keys from config file or environment?
4. Do you need support for other AI services beyond Leonardo/DALL-E?
