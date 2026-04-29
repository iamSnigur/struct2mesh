#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using CraftSharp;
using CraftSharp.Rendering;
using CraftSharp.Resource;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class BlockMeshBuilder
{
    private static readonly Mesh EMPTY_BLOCK_MESH = new();
        
    private static readonly LegacyRandomSource randomSource = new(0L);

    private static readonly BlockMaterialManager s_blockMaterialManager;

    static BlockMeshBuilder()
    {
        s_blockMaterialManager = GameObject
            .FindGameObjectWithTag("MaterialManager")
            .GetComponent<BlockMaterialManager>();
    }
    
    private static void ClearBlockVisual(GameObject modelObject)
    {
        // Clear mesh if present
        if (modelObject.TryGetComponent<MeshFilter>(out var meshFilter))
        {
            meshFilter.sharedMesh = null;
        }
            
        // Clear children if present
        foreach (Transform t in modelObject.transform)
        {
            Object.Destroy(t.gameObject);
        }
    }

    /// <summary>
    /// Build item object from item stack. Returns true if item is not empty and successfully built
    /// </summary>
    public static void BuildBlockGameObject(GameObject modelObject, BlockState blockState, Vector3 pos, byte cullFlags, World world)
    {
        ClearBlockVisual(modelObject);
            
        if (!modelObject.TryGetComponent<MeshFilter>(out var meshFilter))
        {
            meshFilter = modelObject.AddComponent<MeshFilter>();
        }

        if (!modelObject.TryGetComponent<MeshRenderer>(out var meshRenderer))
        {
            meshRenderer = modelObject.AddComponent<MeshRenderer>();
        }

        var blockId = blockState.BlockId;
        int stateId = BlockStatePaletteAdapter.Instance.GetNumIdByObject(blockState);
            
        var packManager = ResourcePackManager.Instance;
        packManager.StateModelTable.TryGetValue(stateId, out var stateModel);
        if (stateModel is null) 
            return;
            
        BlockGeometry? blockGeometry = stateModel.Geometries[0];

        if (BlockEntityTypePalette.INSTANCE.GetBlockEntityForBlock(blockId, out var blockEntityType))
        {
            //client.ChunkRenderManager.CreateBlockEntityRenderForItemModel(modelObject.transform, blockState, blockEntityType);
        }
        
        int color = BlockStatePaletteAdapter.Instance.GetBlockColor(stateId, world, BlockLoc.Zero);
        int waterColor = world.GetWaterColor(BlockLoc.Zero);

        Mesh mesh = BuildBlockMesh_Internal(blockState, blockGeometry, pos,float3.zero, cullFlags, color, waterColor, 0, true);
            
        meshFilter.sharedMesh = mesh;

        if (blockState.InWater)
        {
            meshRenderer.sharedMaterials = new [] {
                s_blockMaterialManager.GetAtlasMaterial(RenderType.WATER),
                s_blockMaterialManager.GetAtlasMaterial(stateModel.RenderType) };
        }
        else
        {
            meshRenderer.sharedMaterial = s_blockMaterialManager.GetAtlasMaterial(stateModel.RenderType);
        }
    }
        
    private static Mesh BuildBlockMesh_Internal(BlockState state, BlockGeometry geometry, Vector3 blockPos, float3 posOffset, int cullFlags, int colorInt, int waterColorInt, byte blockLight, bool includeLiquidMesh)
    {
        int vertexCount = geometry.GetVertexCount(cullFlags);
        int fluidVertexCount = 0;
        int layerMask = 0;
        
        if (state.InLiquid) // Build liquid here
        {
            var liquidNeighborCheck = state.InWater ? BlockNeighborChecks.WATER_SURFACE : BlockNeighborChecks.LAVA_SURFACE;
            int liquidCullFlags = getCullFlags((int)blockPos.x, (int)blockPos.y, (int)blockPos.z, state, liquidNeighborCheck);

            if (liquidCullFlags != 0)
            {
                var liquidLayerIndex = state.InWater ? WATER_LAYER_INDEX : LAVA_LAYER_INDEX;
                fluidVertexCount = FluidGeometry.GetVertexCount(liquidCullFlags);
                vertexCount += fluidVertexCount;
                layerMask |= 1 << liquidLayerIndex;
            }
        }
        
        // Make and set mesh...
        var visualBuffer = new VertexBuffer(vertexCount);

        uint vertexOffset = 0;
        var blockVertLight = Enumerable.Repeat(blockLight, 8).ToArray();

        if (state.InLiquid) // Build liquid here
        {
            BlockNeighborCheck? liquidNeighborCheck = state.InWater ? BlockNeighborChecks.WATER_SURFACE : BlockNeighborChecks.LAVA_SURFACE;
            int liquidCullFlags = getCullFlags((int)blockPos.x, (int)blockPos.y, (int)blockPos.z, state, liquidNeighborCheck);

            if (liquidCullFlags != 0)
            {
                Span<float> liquidHeights = stackalloc float[4];
                getLiquidCornerHeights(state, blockPos, liquidHeights);

                int liquidLayerIndex = state.InWater ? WATER_LAYER_INDEX : LAVA_LAYER_INDEX;
                ResourceLocation liquidTextureStill = FluidGeometry.LiquidTextures[state.InWater ? 0 : 1];
                ResourceLocation liquidTextureFlow  = FluidGeometry.LiquidTextures[state.InWater ? 2 : 3];

                colorInt = state.InWater ? waterColorInt : 0xFFFFFF;

                bool isWat = state.BlockId.Path.Equals("copper_grate");
                
                float3 offset = isWat ? new float3(0.005f) : float3.zero;
                float3 scale = isWat ? new float3(0.99f) : new float3(1f);
                
                FluidGeometry.Build(visualBuffer, ref vertexOffset, offset, scale,
                    liquidTextureStill, liquidTextureFlow, liquidHeights, liquidCullFlags, blockVertLight, colorInt);
            }
        }
        
        int getCullFlags(int x, int y, int z, BlockState self, BlockNeighborCheck check)
        {
            var cullFlags = 0;

            if (check(self, NbtReader.StructureVolume[x, y + 1, z], Direction.Up)) // Up
                cullFlags |= 1 << 0;

            if (check(self, NbtReader.StructureVolume[x, y - 1, z], Direction.Down)) // Down
                cullFlags |= 1 << 1;
                    
            if (check(self, NbtReader.StructureVolume[x + 1, y, z], Direction.South)) // South
                cullFlags |= 1 << 2;

            if (check(self, NbtReader.StructureVolume[x - 1, y, z], Direction.North)) // North
                cullFlags |= 1 << 3;
                    
            if (check(self, NbtReader.StructureVolume[x, y, z + 1], Direction.East)) // East
                cullFlags |= 1 << 4;

            if (check(self, NbtReader.StructureVolume[x, y, z - 1], Direction.West)) // West
                cullFlags |= 1 << 5;
                    
            return cullFlags;
        }
        
        void getLiquidCornerHeights(BlockState self, Vector3 pos, Span<float> heights)
        {
            heights[0] = getLiquidCornerHeight(self, pos, -1, 0);  // NE
            heights[1] = getLiquidCornerHeight(self, pos, 0, 0);   // SE
            heights[2] = getLiquidCornerHeight(self, pos, -1, -1); // NW
            heights[3] = getLiquidCornerHeight(self, pos, 0, -1);  // SW
        }

        float getLiquidCornerHeight(BlockState self, Vector3 pos, int offsetX, int offsetZ)
        {
            for (var ox = offsetX; ox <= offsetX + 1; ox++)
                for (var oz = offsetZ; oz <= offsetZ + 1; oz++)
                    if (IsSameLiquid(self, NbtReader.StructureVolume[pos + new Vector3(ox, 1f, oz)]))
                        return 1f;

            float sumHeight = 0F;
            int count = 0;

            for (var ox = offsetX; ox <= offsetX + 1; ox++)
                for (var oz = offsetZ; oz <= offsetZ + 1; oz++)
                {
                    BlockState? neighbor = NbtReader.StructureVolume[pos + new Vector3(ox, 0f, oz)];

                    if (IsSameLiquid(self, neighbor))
                    {
                        var level = neighbor.LiquidLevel;
                        if (level == 0) return 14F / 16F;

                        sumHeight += GetLiquidBaseHeight(level);
                        count++;
                    }
                    else if (!isLiquidBlockingBlock(neighbor))
                    {
                        count++;
                    }
                }

            if (sumHeight == 0F || count == 0) return 3F / 16F;

            return sumHeight / count;
        }
        
        bool isLiquidBlockingBlock(BlockState state)
        {
            return state is { FullShape: true, NoCollision: false };
        }
        
        geometry.Build(visualBuffer, ref vertexOffset, posOffset, cullFlags, 0, 0F, blockVertLight, colorInt);

        var triIdxCount = vertexCount / 2 * 3;

        var meshDataArr = Mesh.AllocateWritableMeshData(1);
        var meshData = meshDataArr[0];

        var vertAttrs = new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        vertAttrs[0] = new(VertexAttribute.Position,  dimension: 3, stream: 0);
        vertAttrs[1] = new(VertexAttribute.TexCoord0, dimension: 3, stream: 1);
        vertAttrs[2] = new(VertexAttribute.TexCoord3, dimension: 4, stream: 2);
        vertAttrs[3] = new(VertexAttribute.Color,     dimension: 4, stream: 3);

        // Set mesh params
        meshData.SetVertexBufferParams(vertexCount, vertAttrs);
        vertAttrs.Dispose();

        meshData.SetIndexBufferParams(triIdxCount, IndexFormat.UInt32);

        // Set vertex data
        // Positions
        var positions = meshData.GetVertexData<float3>(0);
        positions.CopyFrom(visualBuffer.vert);
        // Tex Coordinates
        var texCoords = meshData.GetVertexData<float3>(1);
        texCoords.CopyFrom(visualBuffer.txuv);
        // Animation Info
        var animInfos = meshData.GetVertexData<float4>(2);
        animInfos.CopyFrom(visualBuffer.uvan);
        // Vertex colors
        var vertColors = meshData.GetVertexData<float4>(3);
        vertColors.CopyFrom(visualBuffer.tint);

        // Set face data
        var triIndices = meshData.GetIndexData<uint>();
        uint vi = 0; var ti = 0;
        for (; vi < vertexCount; vi += 4U, ti += 6)
        {
            triIndices[ti]     = vi;
            triIndices[ti + 1] = vi + 3U;
            triIndices[ti + 2] = vi + 2U;
            triIndices[ti + 3] = vi;
            triIndices[ti + 4] = vi + 1U;
            triIndices[ti + 5] = vi + 3U;
        }

        var bounds = new Bounds(new Vector3(0.5F, 0.5F, 0.5F), new Vector3(1F, 1F, 1F));

        if (includeLiquidMesh && state.InLiquid && !state.IsLava)
        {
            var fluidTriIdxCount = fluidVertexCount / 2 * 3;

            meshData.subMeshCount = 2;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, fluidTriIdxCount)
            {
                bounds = bounds,
                vertexCount = vertexCount
            }, MeshUpdateFlags.DontRecalculateBounds);
            meshData.SetSubMesh(1, new SubMeshDescriptor(fluidTriIdxCount, triIdxCount - fluidTriIdxCount)
            {
                bounds = bounds,
                vertexCount = vertexCount
            }, MeshUpdateFlags.DontRecalculateBounds);
        }
        else
        {
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, triIdxCount)
            {
                bounds = bounds,
                vertexCount = vertexCount
            }, MeshUpdateFlags.DontRecalculateBounds);
        }

        // Create and assign mesh
        var mesh = new Mesh { bounds = bounds };
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArr, mesh);
        // Recalculate mesh normals
        mesh.RecalculateNormals();

        return mesh;
    }
    
    private static bool IsSameLiquid(BlockState self, BlockState neighbor)
    {
        if (!self.InLiquid || !neighbor.InLiquid) return false;
        if (self.InWater) return neighbor.InWater;
        if (self.IsLava) return neighbor.IsLava;

        return self.FluidStateId == neighbor.FluidStateId;
    }
    
    private static float GetLiquidBaseHeight(byte level)
    {
        return level >= 8 ? 1F : (14F - level * 1.9F) / 16F;
    }

    private const int LAVA_LAYER_INDEX = 1;
    private const int WATER_LAYER_INDEX = 0;

    public static BlockEntityRender CreateBlockEntityRenderForItemModel(Transform parent, BlockState blockState, BlockEntityType blockEntityType)
    {
        GameObject blockEntityPrefab = GetPrefabForType(blockEntityType.TypeId);
        var blockEntityObj = Object.Instantiate(blockEntityPrefab, parent);
        var blockEntityRender = blockEntityObj!.GetComponent<BlockEntityRender>();

        blockEntityObj.name = $"[Item Model] {blockEntityType}";
        blockEntityObj.transform.localPosition = Vector3.zero;
        blockEntityObj.transform.localEulerAngles = Vector3.zero;

        blockEntityRender.Initialize(null, blockState, blockEntityType, new());
                
        return blockEntityRender;
    }
    
    [SerializeField] private static GameObject defaultPrefab;
        
    [SerializeField] private static GameObject chestPrefab;
    [SerializeField] private static GameObject shulkerBoxPrefab;
    [SerializeField] private static GameObject bannerPrefab;
    [SerializeField] private static GameObject signPrefab;
    [SerializeField] private static GameObject hangingSignPrefab;
    
    private static readonly Dictionary<ResourceLocation, GameObject> s_blockEntityPrefabs = new();
    
    private static GameObject GetPrefabForType(ResourceLocation type)
    {
        return s_blockEntityPrefabs.GetValueOrDefault(type, defaultPrefab);
    }
}