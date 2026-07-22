using System;
using System.Collections.Generic;
using System.Linq;
using Core.AssetManagement;
using Core.Async;
using CraftSharp;
using Cysharp.Threading.Tasks;
using Logic.Parser;
using Logic.Parser.Model;
using Logic.Utils;
using UI.Views;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Logic
{
    public class StructureBuilder : MonoBehaviour
    {
        public bool SeparateMeshes;
        
        public GameObject Root { get; private set; }
        public static StructureVolume StructureVolume;
    
        private CameraController _cameraController;
        private LoadingCurtainView _loadingCurtainView;

        private ParserRegistry _parserRegistry;
        
        public void InjectDependencies(
            LoadingCurtainView loadingCurtainView,
            CameraController cameraController)
        {
            _loadingCurtainView = loadingCurtainView;
            _cameraController = cameraController;
        }
        
        private void Awake()
        {
            Root = new GameObject("Structure");
            MeshFilter meshFilter = Root.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh {name = "StructureMesh"};
            Root.AddComponent<MeshRenderer>();
            StructureVolume = new StructureVolume();
            _parserRegistry = new ParserRegistry();
        }
    
        public async UniTaskVoid BuildFromBytesAsync(byte[] rawBytes, string fileType)
        {
            _cameraController.InputDisabled = true;
            Root.SetActive(false);
            _loadingCurtainView.SetProgressText("Looking at your structure...");
            _loadingCurtainView.Show();
            
            UniTask delay1 = UniTask.Delay(250);
            ParsedStructure structure = null;
            
            try
            {
                IParser parser = _parserRegistry.GetFor(fileType);
                UniTask<ParsedStructure> parseTask = parser.Parse(rawBytes).Preserve();

                await UniTask.WhenAll(delay1, parseTask);
                structure = await parseTask;
            }
            catch (ArgumentException e)
            {
                Debug.LogError(e);
                _loadingCurtainView.SetProgressText("Structure format is not supported");
                await UniTask.Delay(1500);
                _loadingCurtainView.Hide();
                return;
            }
            catch (OverflowException e)
            {
                Debug.LogError(e);
                _loadingCurtainView.SetProgressText("Structure exceeded block limit");
                await UniTask.Delay(1500);
                _loadingCurtainView.Hide();
                return;
            }
            
            _loadingCurtainView.SetProgressText("Collecting resources...");
            
            UniTask delay2 = UniTask.Delay(250);
            UniTask updateVolumeTask = UpdateStructureVolumeAsync(structure);

            await UniTask.WhenAll(delay2, updateVolumeTask);
            
            MinecraftAssetProvider.ClearBlockStatePalette();
            await MinecraftAssetProvider.UpdateBlockStatePalette(structure.Palette);
            MinecraftAssetProvider.UpdateColorRules();
            
            _loadingCurtainView.SetProgressText("Building...");
            UniTask delay3 = UniTask.Delay(1000);
            UniTask constructTask = Construct(structure);

            await UniTask.WhenAll(
                delay3.ContinueWith(() => 
                    _loadingCurtainView.SetProgressText("Building a massive structure...")),
                constructTask);
            
            _loadingCurtainView.SetProgressText("Done");

            await UniTask.Delay(300);
            
            Root.SetActive(true);
            _loadingCurtainView.Hide();
            _cameraController
                .DoOrbitAnimation(
                    Root.GetComponent<Renderer>().bounds,
                    2f,
                    () => _cameraController.InputDisabled = false)
                .Forget();
        }

        private async UniTask UpdateStructureVolumeAsync(ParsedStructure structure)
        {
            await UniTask.Yield();
            
            StructureVolume.Reset();

            LongTaskYielder yielder = new();
            
            foreach (BlockEntry block in structure.Blocks)
            {
                StructureVolume[block.Pos] = CreateBlockState(structure.Palette[block.State]);

                await yielder.TryYieldAsync();
            }
        }
        
        private static BlockState CreateBlockState(PaletteEntry entry)
        {
            if (entry.Name.Equals("air"))
                return BlockState.AIR_STATE;
        
            ResourceLocation? fluidState = entry.Name switch
            {
                "kelp" => BlockState.WATER_ID,
                "kelp_plant" => BlockState.WATER_ID,
                "seagrass" => BlockState.WATER_ID,
                "tall_seagrass" => BlockState.WATER_ID,
                "water" => BlockState.WATER_ID,
                "bubble_column" => BlockState.WATER_ID,
                "lava" => BlockState.LAVA_ID,
                _ => null
            };

            bool noOcclusion = BlockStateHelper.GetRenderTypeFor(entry.Name) is not RenderType.SOLID;
        
            BlockState blockState = new(
                new ResourceLocation(entry.Name),
                1,
                1,
                false,
                0b111111,
                true,
                noOcclusion,
                15,
                0,
                fluidState,
                entry.Properties);

            return blockState;
        }
    
        private static void ClearVisual(GameObject modelObject)
        {
            if (modelObject.TryGetComponent<MeshFilter>(out var meshFilter))
            {
                meshFilter.sharedMesh.Clear();
                //meshFilter.sharedMesh = null;
            }
        }
    
        private async UniTask Construct(ParsedStructure structure)
        {
            ClearVisual(Root);
            
            MinecraftAssetProvider.ReleaseBlockStateHandles();
        
            List<UniTask> warmupTasks = new(structure.Palette.Count);

            LongTaskYielder yielder = new();
            
            foreach (PaletteEntry entry in structure.Palette)
            {
                warmupTasks.Add(MinecraftAssetProvider.WarmupBlockGeometriesAsync(entry.Name));
                await yielder.TryYieldAsync();
            }
            
            await UniTask.WhenAll(warmupTasks);

            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(structure.Blocks.Count);
            List<BuildMesh> buildMeshes = new List<BuildMesh>(structure.Blocks.Count);
            
            for (int i = 0; i < meshDataArray.Length; i++)
            {
                BlockEntry blockEntry = structure.Blocks[i];
                PaletteEntry paletteEntry = structure.Palette[blockEntry.State];
                
                if (IsBlockTransparent(paletteEntry))
                    continue;
                
                byte cullFlags = StructureVolume.GetCullFlagsFor(blockEntry.Pos);
            
                Mesh.MeshData meshData = meshDataArray[i];
                BuildMesh buildMeshModel = MinecraftAssetProvider.BuildModel(ref meshData, paletteEntry.Name, blockEntry.Pos, paletteEntry.Properties, cullFlags);
                buildMeshes.Add(buildMeshModel);

                await yielder.TryYieldAsync();
            }

            int blockCount = buildMeshes.Count;
            var blockVertexOffset = new int[blockCount];
            int totalVertexCount = 0;

            for (int i = 0; i < blockCount; i++)
            {
                blockVertexOffset[i] = totalVertexCount;
                totalVertexCount += buildMeshes[i].MeshData.vertexCount;
            }

            // --- Pass 2: group (blockIndex, submeshIndex) by material, compute total index count ---
            Dictionary<Material, List<(int blockIndex, int subIndex)>> submeshGroups = new();
            int totalIndexCount = 0;

            for (int i = 0; i < blockCount; i++)
            {
                BuildMesh bm = buildMeshes[i];
                for (int s = 0; s < bm.Materials.Length; s++)
                {
                    Material mat = bm.Materials[s];
                    if (!submeshGroups.TryGetValue(mat, out var list))
                        submeshGroups[mat] = list = new List<(int, int)>();
                    list.Add((i, s));

                    totalIndexCount += (int)bm.MeshData.GetSubMesh(s).indexCount;
                }
            }

            Material[] finalMaterials = submeshGroups.Keys.ToArray(); // final submesh order

            // --- Allocate output MeshData ---
            Mesh.MeshDataArray outputArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData outData = outputArray[0];

            var outVertAttrs = new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            outVertAttrs[0] = new VertexAttributeDescriptor(VertexAttribute.Position,  dimension: 3, stream: 0);
            outVertAttrs[1] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 3, stream: 1);
            outVertAttrs[2] = new VertexAttributeDescriptor(VertexAttribute.TexCoord3, dimension: 4, stream: 2);
            outVertAttrs[3] = new VertexAttributeDescriptor(VertexAttribute.Color,     dimension: 4, stream: 3);

            outData.SetVertexBufferParams(totalVertexCount, outVertAttrs);
            outVertAttrs.Dispose();

            outData.SetIndexBufferParams(totalIndexCount, IndexFormat.UInt32);
            outData.subMeshCount = finalMaterials.Length;

            NativeArray<float3> outPositions  = outData.GetVertexData<float3>(0);
            NativeArray<float3> outTexCoords  = outData.GetVertexData<float3>(1);
            NativeArray<float4> outAnimInfos  = outData.GetVertexData<float4>(2);
            NativeArray<float4> outVertColors = outData.GetVertexData<float4>(3);
            NativeArray<uint> outIndices    = outData.GetIndexData<uint>();

            // --- Copy vertex buffers, once per block ---
            for (int i = 0; i < blockCount; i++)
            {
                Mesh.MeshData srcData = buildMeshes[i].MeshData;
                int offset = blockVertexOffset[i];
                int count = srcData.vertexCount;

                NativeArray<float3>.Copy(srcData.GetVertexData<float3>(0), 0, outPositions, offset, count);
                NativeArray<float3>.Copy(srcData.GetVertexData<float3>(1), 0, outTexCoords, offset, count);
                NativeArray<float4>.Copy(srcData.GetVertexData<float4>(2), 0, outAnimInfos, offset, count);
                NativeArray<float4>.Copy(srcData.GetVertexData<float4>(3), 0, outVertColors, offset, count);
            }

            // --- Copy indices, grouped by material, rebased by block's vertex offset ---
            int indexCursor = 0;

            for (int m = 0; m < finalMaterials.Length; m++)
            {
                int submeshIndexStart = indexCursor;

                foreach (var (blockIndex, subIndex) in submeshGroups[finalMaterials[m]])
                {
                    Mesh.MeshData srcData = buildMeshes[blockIndex].MeshData;
                    SubMeshDescriptor sub = srcData.GetSubMesh(subIndex);
                    NativeArray<uint> srcIndices = srcData.GetIndexData<uint>();

                    uint vOffset = (uint)blockVertexOffset[blockIndex];

                    for (int idx = 0; idx < sub.indexCount; idx++)
                        outIndices[indexCursor + idx] = srcIndices[sub.indexStart + idx] + vOffset;

                    indexCursor += sub.indexCount;
                }

                outData.SetSubMesh(m, new SubMeshDescriptor(submeshIndexStart, indexCursor - submeshIndexStart),
                    MeshUpdateFlags.DontRecalculateBounds);
            }

            Mesh finalMesh = Root.GetComponent<MeshFilter>().sharedMesh;
            
            Mesh.ApplyAndDisposeWritableMeshData(outputArray, finalMesh);
            finalMesh.bounds = new Bounds(structure.Size / 2f, structure.Size);
            finalMesh.RecalculateNormals();
            
            Root.GetComponent<MeshRenderer>().sharedMaterials = finalMaterials;

            meshDataArray.Dispose();
            
            /*foreach (BlockEntry block in structure.Blocks)
            {
                PaletteEntry paletteEntry = structure.Palette[block.State];

                if (IsBlockTransparent(paletteEntry))
                    continue;

                byte cullFlags = StructureVolume.GetCullFlagsFor(block.Pos);
            
                // get mesh and materials
                // check debug
                //  create debug objects
                // append to dictionary
                // send to mesh combiner
                
                // pass mesh data struct
                GameObject blockView = MinecraftAssetProvider.BuildModel(paletteEntry.Name, block.Pos, paletteEntry.Properties, cullFlags);
                Transform cachedTransform = blockView.transform;
                cachedTransform.parent = Root.transform;
                cachedTransform.position = block.Pos;

                await yielder.TryYieldAsync();
            }
            
            MeshCombiner meshCombiner = Root.GetComponent<MeshCombiner>();
            meshCombiner ??= Root.AddComponent<MeshCombiner>();
            
            meshCombiner.CreateMultiMaterialMesh = true;
            meshCombiner.DestroyCombinedChildren = true;
            await meshCombiner.CombineMeshes(false);*/
            
            Renderer component = Root.GetComponent<Renderer>();
            _cameraController.SetPivot(component.bounds.center);
        }

        private static bool IsBlockTransparent(PaletteEntry paletteEntry) =>
            paletteEntry.Name.Equals("air") 
            || paletteEntry.Name.Equals("cave_air")
            || paletteEntry.Name.Equals("structure_void")
            || paletteEntry.Name.Equals("barrier");
    }
}