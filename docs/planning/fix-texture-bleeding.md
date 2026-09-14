# Fixing Texture Bleeding on Block Edges

## Problem

Visible seams appear along block edges, most noticeably where adjacent blocks have high-contrast textures.

**Cause:** all blocks and block entities share a single, tightly packed texture atlas. When the GPU interpolates UVs across a mesh and samples near the edge of a block's region, filtering pulls in texels from the neighboring texture in the atlas.

## New Solution

Move blocks into a dedicated atlas where every block texture is surrounded by a **16px border filled by extruding its edge pixels**.

Any sample that spills past a tile's edge now lands on a copy of that tile's own edge pixels, so no foreign color leaks in.

The 16px border is required for good-looking mipmaps. Each mip level halves the border (16 → 8 → 4 → 2 → 1 px), so tiles stay bleed-free down to mip level 4.

Block entities remain in the existing atlas with their current settings.

## Implementation Plan

### Step 1: Split texture sources
- Separate textures into bundles: blocks and block entities.

### Step 2: Generate two atlases
- Update atlas generation to produce two atlases with independent parameters:
  - **Block atlas:** 16px extruded padding, mipmaps enabled
  - **Block entity atlas:** nothing changes
- Inspect the generated static block atlas:
  - Borders are extruded correctly on all four sides
  - UVs point to the inner tile, not the padded area
  - Tile positions are aligned to multiples of 16px so borders stay intact across mip levels

### Step 3: Create a material per atlas
- Create a separate material for each atlas.
- Assign materials to the corresponding meshes.

### Step 4: Test and tune
- Check high-contrast block neighbors both up close and at a distance.
- Verify each mip level visually.
- Test in the **WebGL** build.

## Definition of Done

No visible edge bleeding in the WebGL build.

## Notes

- **Atlas size:** with 16×16 block textures, each padded tile becomes 48×48, which is 9× the area. Confirm the atlas still fits within target texture size limits.
- **Tiled UVs:** if the mesher ever merges faces and repeats UVs across them (greedy meshing), atlas-based tiles won't wrap correctly. That case would need a different approach.
- **Alternative:** if the platform supports WebGL2, a texture array (one layer per block) eliminates bleeding entirely without any padding. It's worth considering if the atlas approach falls short.