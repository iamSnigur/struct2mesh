using System;
using System.Collections.Generic;
using Core.AssetManagement;
using Core.Async;
using CraftSharp;
using Cysharp.Threading.Tasks;
using Logic.Parser.Model;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Logic.MeshGenerator
{
    public class SimpleMeshGenerator : IMeshGenerator, IDisposable
    {
        private readonly LongTaskYielder _taskYielder = new();
        private bool _busy;

        private readonly List<BlockRenderData> _blockRenderDataList = new();
        private int[] _blockVertexOffset = Array.Empty<int>();

        private sealed class SubmeshBucket
        {
            public readonly List<(int blockIndex, int subIndex)> Entries = new();
            public int LastRound = -1;
        }

        private readonly Dictionary<Material, SubmeshBucket> _submeshBuckets = new();
        private readonly List<Material> _materialOrder = new();
        private Material[] _finalMaterialsBuffer = Array.Empty<Material>();
        private int _round;

        private NativeArray<VertexAttributeDescriptor> _vertexAttributes;

        public SimpleMeshGenerator()
        {
            _vertexAttributes = new NativeArray<VertexAttributeDescriptor>(4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _vertexAttributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position,  dimension: 3, stream: 0);
            _vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 3, stream: 1);
            _vertexAttributes[2] = new VertexAttributeDescriptor(VertexAttribute.TexCoord3, dimension: 4, stream: 2);
            _vertexAttributes[3] = new VertexAttributeDescriptor(VertexAttribute.Color,     dimension: 4, stream: 3);
        }

        public async UniTask GenerateMesh(MeshFilter meshFilter, MeshRenderer meshRenderer, ParsedStructure structure, StructureVolume structureVolume)
        {
            if (_busy)
                throw new InvalidOperationException("Mesh generator is busy!");

            _busy = true;
            try
            {
                await GenerateMeshInternal(meshFilter, meshRenderer, structure, structureVolume);
            }
            finally
            {
                _busy = false;
            }
        }

        private async UniTask GenerateMeshInternal(MeshFilter meshFilter, MeshRenderer meshRenderer, ParsedStructure structure, StructureVolume structureVolume)
        {
            _round++;

            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(structure.Blocks.Count);

            _blockRenderDataList.Clear();
            if (_blockRenderDataList.Capacity < structure.Blocks.Count)
                _blockRenderDataList.Capacity = structure.Blocks.Count;

            for (int i = 0; i < meshDataArray.Length; i++)
            {
                BlockEntry blockEntry = structure.Blocks[i];
                PaletteEntry paletteEntry = structure.Palette[blockEntry.State];

                if (IsBlockTransparent(paletteEntry))
                    continue;

                byte cullFlags = structureVolume.GetCullFlagsFor(blockEntry.Pos);

                Mesh.MeshData meshData = meshDataArray[i];
                BlockRenderData blockRenderData = MinecraftAssetProvider.BuildBlock(ref meshData, paletteEntry.Name, blockEntry.Pos, paletteEntry.Properties, cullFlags);
                _blockRenderDataList.Add(blockRenderData);

                await _taskYielder.TryYieldAsync();
            }

            int blockCount = _blockRenderDataList.Count;

            if (_blockVertexOffset.Length < blockCount)
                _blockVertexOffset = new int[blockCount];

            int totalVertexCount = 0;

            for (int i = 0; i < blockCount; i++)
            {
                _blockVertexOffset[i] = totalVertexCount;
                totalVertexCount += _blockRenderDataList[i].MeshData.vertexCount;
            }

            _materialOrder.Clear();
            int totalIndexCount = 0;

            for (int i = 0; i < blockCount; i++)
            {
                BlockRenderData renderData = _blockRenderDataList[i];
                for (int s = 0; s < renderData.Materials.Length; s++)
                {
                    Material mat = renderData.Materials[s];

                    if (!_submeshBuckets.TryGetValue(mat, out SubmeshBucket bucket))
                    {
                        bucket = new SubmeshBucket();
                        _submeshBuckets[mat] = bucket;
                    }

                    if (bucket.LastRound != _round)
                    {
                        bucket.Entries.Clear();
                        bucket.LastRound = _round;
                        _materialOrder.Add(mat);
                    }

                    bucket.Entries.Add((i, s));
                    totalIndexCount += renderData.MeshData.GetSubMesh(s).indexCount;
                }
            }

            if (_finalMaterialsBuffer.Length != _materialOrder.Count)
                _finalMaterialsBuffer = new Material[_materialOrder.Count];
            _materialOrder.CopyTo(_finalMaterialsBuffer);

            Mesh.MeshDataArray outputArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData outData = outputArray[0];

            outData.SetVertexBufferParams(totalVertexCount, _vertexAttributes);
            outData.SetIndexBufferParams(totalIndexCount, IndexFormat.UInt32);
            outData.subMeshCount = _finalMaterialsBuffer.Length;

            NativeArray<float3> outPositions  = outData.GetVertexData<float3>(0);
            NativeArray<float3> outTexCoords  = outData.GetVertexData<float3>(1);
            NativeArray<float4> outAnimInfos  = outData.GetVertexData<float4>(2);
            NativeArray<float4> outVertColors = outData.GetVertexData<float4>(3);
            NativeArray<uint> outIndices      = outData.GetIndexData<uint>();

            for (int i = 0; i < blockCount; i++)
            {
                Mesh.MeshData srcData = _blockRenderDataList[i].MeshData;
                int offset = _blockVertexOffset[i];
                int count = srcData.vertexCount;

                NativeArray<float3>.Copy(srcData.GetVertexData<float3>(0), 0, outPositions, offset, count);
                NativeArray<float3>.Copy(srcData.GetVertexData<float3>(1), 0, outTexCoords, offset, count);
                NativeArray<float4>.Copy(srcData.GetVertexData<float4>(2), 0, outAnimInfos, offset, count);
                NativeArray<float4>.Copy(srcData.GetVertexData<float4>(3), 0, outVertColors, offset, count);
            }

            int indexCursor = 0;

            for (int m = 0; m < _finalMaterialsBuffer.Length; m++)
            {
                int submeshIndexStart = indexCursor;
                List<(int blockIndex, int subIndex)> entries = _submeshBuckets[_finalMaterialsBuffer[m]].Entries;

                for (int e = 0; e < entries.Count; e++)
                {
                    (int blockIndex, int subIndex) = entries[e];
                    Mesh.MeshData srcData = _blockRenderDataList[blockIndex].MeshData;
                    SubMeshDescriptor sub = srcData.GetSubMesh(subIndex);
                    NativeArray<uint> srcIndices = srcData.GetIndexData<uint>();

                    uint vOffset = (uint)_blockVertexOffset[blockIndex];

                    for (int idx = 0; idx < sub.indexCount; idx++)
                        outIndices[indexCursor + idx] = srcIndices[sub.indexStart + idx] + vOffset;

                    indexCursor += sub.indexCount;
                }

                outData.SetSubMesh(m,
                    new SubMeshDescriptor(submeshIndexStart, indexCursor - submeshIndexStart),
                    MeshUpdateFlags.DontRecalculateBounds);
            }

            Mesh mesh = meshFilter.sharedMesh;
            Mesh.ApplyAndDisposeWritableMeshData(outputArray, mesh);
            mesh.bounds = new Bounds(structure.Size / 2f, structure.Size);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshRenderer.sharedMaterials = _finalMaterialsBuffer;

            meshDataArray.Dispose();
        }

        private static bool IsBlockTransparent(PaletteEntry paletteEntry) =>
            paletteEntry.Name.Equals("air")
            || paletteEntry.Name.Equals("cave_air")
            || paletteEntry.Name.Equals("structure_void")
            || paletteEntry.Name.Equals("barrier");

        public void Dispose()
        {
            if (_vertexAttributes.IsCreated)
                _vertexAttributes.Dispose();
        }
    }
}