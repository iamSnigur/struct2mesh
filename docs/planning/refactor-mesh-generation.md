# Mesh Generation Refactor

## Current Implementation
The current mesh generation pipeline creates a large number of `GameObjects`, each with a `MeshFilter` and `MeshRenderer`.<br>
These meshes are then combined into a single mesh by `MeshCombiner.cs`, after which the temporary objects are destroyed.

## Problems
The current implementations has several drawbacks:
- Frequent creation and destruction of `GameObject` and `Mesh` objects increases CPU overhead;
- Large and frequent number of allocation in managed heap leads to memory fragmenation;
- WebGL does not support heap defragmentation, so repeatedly loading large structures can eventually lead to out-of-memory due to fragmentation.

## Goals
The primary goal is to reduce unnecessary managed heap allocations, so it can prevent memory fragmentation and also make able to load larger structures.

The secondary goal is to make mesh generation multithreaded and improve generation time.

## Research
### Creating final mesh without unnecessary heap allocations

**New Pipeline**
1. Allocate a `MeshDataArray`;
2. Generates each block mesh using `Job System`;
3. Group generated mesh by `Materials` (multiple `MeshData` per `Material`);
4. Merge each material's mesh group into single `MeshData`;
5. Apply submeshes to final single `Mesh` object.

### Details
> `MeshData` and `MeshDataArray` are designed to support efficient mesh generation, particularly with the Unity `Job System`. They allow mesh data to be generated without creating temporary Mesh objects. Native memory for vertex and index buffers is still allocated as required.

> Mesh generation jobs can run in parallel when multithreading is available. On WebGL, this requires enabling `Native C/C++ Multithreading`, which relies on browser support for WebAssembly threads.

> WebAssembly multithreading requires browsers that support SharedArrayBuffer and WebAssembly threads. Users on unsupported browsers will fall back to single-threaded execution.

> Multithreading setting also requires specific secure server headers. The Web platform supports multithreading when document is within a secure context.<br>
  *Cross-Origin-Opener-Policy: same-origin<br>
  Cross-Origin-Embedder-Policy: require-corp<br>
  Cross-Origin-Resource-Policy: cross-origin<br>*

## Change Notes

Model geometry is currently retrieved through `ResourcePackManager.cs`. Since this data will be accessed from jobs, it may need to be reorganized into job-compatible data structures.

Refactoring the current implementation to fully support the `Job System` may require significant effort.<br>
The first step should be migrating mesh generation to use `MeshDataArray` in order to reduce unnecessary allocations before introducing multithreading.
