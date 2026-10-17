# Struct2Mesh

Browser-based tool that converts Minecraft structure files (`.nbt`) into 3D models. Drop a file, preview it, export a mesh.

## Usage

1. Open https://struct2mesh.pages.dev/
2. Drop or upload a `.nbt` structure file.
3. Preview and export.

## Supported

- **Input:** `.nbt` structure files (Minecraft Java). Unknown blocks are replaced with a fallback block.
- **Output:** FBX (ASCII), GLB, OBJ, PLY
- **Size:** up to 200,000 blocks in volume

## Limitations

- Entities are ignored on import.
- Block entity visuals driven by NBT data (banner patterns, pottery patterns) are not rendered.
- Texture animations play in the viewer but export as static textures.
- Overlapping transparent blocks (glass, water, ice) might be sorted incorrectly in exported models.

## Tech stack

| Area | Tech |
| --- | --- |
| Engine | Unity 6 (`6000.3.9f1`), URP, WebGL target |
| UI | UI Toolkit (UXML / USS) |
| Assets | Addressables (temporary bundles are build as remote, so they are not included in the build) |
| Browser interop | `.jslib` bridges: drag & drop, clipboard, scroll wheel, zip download |
| Hosting | Cloudflare Pages |

## Dependencies

- [VContainer](https://github.com/hadashiA/VContainer) - DI framework package
- [UniTask](https://github.com/Cysharp/UniTask) - Allocation-free async package
- [fNbt](https://www.nuget.org/packages/fNbt) - Package for reading `.nbt` files
- [UnityGLTF](https://github.com/KhronosGroup/UnityGLTF) - Package for exporting `glb` models
- [CraftSharp-Resource](https://github.com/DevBobcorn/CraftSharp-Resource) - Resource pack loading and block model building. This package is heavily modified for Struct2Mesh needs (CDDL-1.0)

## Running locally

1. Open `src/minecraft.structure.loader` in Unity `6000.3.9f1`.
2. Open `Assets/Scenes/BootScene.unity` and press Play.
3. You can open any structure file within Unity Editor by right-clicking it and selecting `Load NBT Structure` in menu. Check `Assets/Structures/` for test structures.
> Unlike the browser-based version, local editor version does not apply block limit check, so you can load and export bigger structures

## License

[Apache 2.0](LICENSE)

Not affiliated with or endorsed by Mojang Studios. Minecraft assets are the property of their respective owners and are not covered by this license.
