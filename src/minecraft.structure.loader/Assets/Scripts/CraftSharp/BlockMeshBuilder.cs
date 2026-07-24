#nullable enable
using System;
using System.Linq;
using Core;
using CraftSharp.Resource;
using Logic;
using Logic.Utils;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CraftSharp
{
    public static class BlockMeshBuilder
    {
        private static readonly MaterialManager s_materialManager;
        private static readonly Material s_fallbackMaterial;

        static BlockMeshBuilder()
        {
            s_materialManager = Object.FindFirstObjectByType<MaterialManager>();
            s_fallbackMaterial = CreateFallbackMaterial();
        }

        private static Material CreateFallbackMaterial() => 
            Resources.Load<Material>("FallbackMaterial");

        private static void CreateFallbackMesh(ref Mesh.MeshData meshData, float3 posOffset)
        {
            NativeArray<VertexAttributeDescriptor> vertAttrs = new(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            vertAttrs[0] = new VertexAttributeDescriptor(VertexAttribute.Position,  dimension: 3, stream: 0);
            vertAttrs[1] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 3, stream: 1);
            vertAttrs[2] = new VertexAttributeDescriptor(VertexAttribute.TexCoord3, dimension: 4, stream: 2);
            vertAttrs[3] = new VertexAttributeDescriptor(VertexAttribute.Color,     dimension: 4, stream: 3);

            meshData.SetVertexBufferParams(8, vertAttrs);
            vertAttrs.Dispose();

            meshData.SetIndexBufferParams(36, IndexFormat.UInt32);

            NativeArray<float3> positions = meshData.GetVertexData<float3>(0);
            positions[0] = new float3(0, 0, 0) + posOffset;
            positions[1] = new float3(0, 0, 1) + posOffset;
            positions[2] = new float3(0, 1, 1) + posOffset;
            positions[3] = new float3(0, 1, 0) + posOffset;
            positions[4] = new float3(1, 0, 0) + posOffset;
            positions[5] = new float3(1, 0, 1) + posOffset;
            positions[6] = new float3(1, 1, 1) + posOffset;
            positions[7] = new float3(1, 1, 0) + posOffset;

            NativeArray<float3> texCoords = meshData.GetVertexData<float3>(1);
            for (int i = 0; i < 8; i++)
                texCoords[i] = float3.zero;

            NativeArray<float4> animInfos = meshData.GetVertexData<float4>(2);
            for (int i = 0; i < 8; i++)
                animInfos[i] = float4.zero;

            NativeArray<float4> vertColors = meshData.GetVertexData<float4>(3);
            for (int i = 0; i < 8; i++)
                vertColors[i] = new float4(1, 1, 1, 1);

            NativeArray<uint> indices = meshData.GetIndexData<uint>();
            uint[] tris =
            {
                0, 1, 2,
                0, 2, 3,
                5, 4, 7,
                5, 7, 6,
                4, 0, 3,
                4, 3, 7,
                1, 5, 6,
                1, 6, 2,
                7, 3, 2,
                7, 2, 6,
                5, 1, 0,
                5, 0, 4
            };
            for (int i = 0; i < tris.Length; i++)
                indices[i] = tris[i];

            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, 36)
            {
                vertexCount = 8
            }, MeshUpdateFlags.DontRecalculateBounds);
        }
        
        public static BlockRenderData BuildMeshData(ref Mesh.MeshData meshData, BlockState blockState, Vector3 pos, byte cullFlags, World world)
        {
            BlockRenderData blockRenderData;
            int stateId = BlockStatePaletteAdapter.Instance.GetNumIdByObject(blockState);
            
            var packManager = ResourcePackManager.Instance;
            packManager.StateModelTable.TryGetValue(stateId, out var stateModel);
            if (stateModel is null)
            {
                CreateFallbackMesh(ref meshData, (float3)pos);
                blockRenderData.MeshData = meshData;
                blockRenderData.Materials = new[] { s_fallbackMaterial };
                return blockRenderData;
            }
            
            BlockGeometry? blockGeometry = stateModel.Geometries[0];

            int color = BlockStatePaletteAdapter.Instance.GetBlockColor(stateId, world, BlockLoc.Zero);
            int waterColor = world.GetWaterColor(BlockLoc.Zero);

            BuildBlockMesh_Internal(ref meshData, blockState, blockGeometry, pos, (float3)pos, cullFlags, color, waterColor, 1, true);

            blockRenderData.MeshData = meshData;
            blockRenderData.Materials = new Material[meshData.subMeshCount];

            if (blockState.InWater)
            {
                blockRenderData.Materials = new [] 
                {
                    s_materialManager.GetAtlasMaterial(RenderType.WATER),
                    s_materialManager.GetAtlasMaterial(stateModel.RenderType) 
                };
            }
            else
            {
                blockRenderData.Materials = new []
                {
                    s_materialManager.GetAtlasMaterial(stateModel.RenderType)
                };
            }

            return blockRenderData;
        }
        
        private static void BuildBlockMesh_Internal(
            ref Mesh.MeshData meshData, 
            BlockState state,
            BlockGeometry geometry,
            Vector3 blockPos,
            float3 posOffset,
            int cullFlags, int colorInt, int waterColorInt,
            byte blockLight, bool includeLiquidMesh)
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

                    int fluidColorInt = state.InWater ? waterColorInt : 0xFFFFFF;

                    bool isWaterloggedCutout = BlockStateHelper.IsWaterloggedCutout(state.BlockId.Path);
                
                    float3 offset = isWaterloggedCutout ? new float3(0.005f) : float3.zero;
                    float3 scale = isWaterloggedCutout ? new float3(0.99f) : new float3(1f);
                
                    FluidGeometry.Build(
                        visualBuffer, 
                        ref vertexOffset,
                        offset + posOffset,
                        scale,
                        liquidTextureStill, 
                        liquidTextureFlow,
                        liquidHeights,
                        liquidCullFlags,
                        blockVertLight,
                        fluidColorInt);
                }
            }
        
            int getCullFlags(int x, int y, int z, BlockState self, BlockNeighborCheck check)
            {
                var cullFlags = 0;

                if (check(self, StructureBuilder.StructureVolume[x, y + 1, z], Direction.Up)) // Up
                    cullFlags |= 1 << 0;

                if (check(self, StructureBuilder.StructureVolume[x, y - 1, z], Direction.Down)) // Down
                    cullFlags |= 1 << 1;
                    
                if (check(self, StructureBuilder.StructureVolume[x + 1, y, z], Direction.South)) // South
                    cullFlags |= 1 << 2;

                if (check(self, StructureBuilder.StructureVolume[x - 1, y, z], Direction.North)) // North
                    cullFlags |= 1 << 3;
                    
                if (check(self, StructureBuilder.StructureVolume[x, y, z + 1], Direction.East)) // East
                    cullFlags |= 1 << 4;

                if (check(self, StructureBuilder.StructureVolume[x, y, z - 1], Direction.West)) // West
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
                        if (IsSameLiquid(self, StructureBuilder.StructureVolume[pos + new Vector3(ox, 1f, oz)]))
                            return 1f;

                float sumHeight = 0F;
                int count = 0;

                for (var ox = offsetX; ox <= offsetX + 1; ox++)
                    for (var oz = offsetZ; oz <= offsetZ + 1; oz++)
                    {
                        BlockState? neighbor = StructureBuilder.StructureVolume[pos + new Vector3(ox, 0f, oz)];

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
    }
}