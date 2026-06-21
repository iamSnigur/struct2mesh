using System;
using System.Collections;
using System.Collections.Generic;
using Core.AssetManagement;
using Core.Utils;
using CraftSharp;
using Cysharp.Threading.Tasks;
using fNbt;
using UnityEngine;

namespace Core
{
    public class NbtStructureParser : MonoBehaviour
    {
        public LoaderCurtain LoaderCurtain;
        public string StructureNbtFilePath;
        public CameraController CameraController;
    
        public GameObject Root { get; private set; }
        public static StructureVolume StructureVolume;
    
        private void Awake()
        {
            Root = new GameObject("Structure");
            StructureVolume = new StructureVolume();
        }

#if UNITY_EDITOR
        private IEnumerator Start()
        {
            while (!MinecraftAssetProvider.Loaded)
                yield return null;

            LoadNbtStructure();
        }
#endif

        [ContextMenu("LoadNbtStructure")]
        public void LoadNbtStructure()
        {
            NbtFile nbtFile = new();
            nbtFile.LoadFromFile(StructureNbtFilePath);

            Parse(nbtFile).Forget();
        }
    
        public async UniTaskVoid Parse(NbtFile nbtFile)
        {
            if (!IsFileNbtStructure(nbtFile))
            {
                Debug.Log("File is not a nbt structure!");
                return;
            }

            CameraController.InputDisabled = true;
            Root.SetActive(false);
            LoaderCurtain.UpdateProgressLabel("Looking at your structure...");
            LoaderCurtain.Show();
            
            UniTask delay1 = UniTask.Delay(750);
            UniTask<Structure> parseTask = ParseNbtStructure(nbtFile).Preserve();

            await UniTask.WhenAll(delay1, parseTask);
            Structure structure = await parseTask;
            
            LoaderCurtain.UpdateProgressLabel("Gathering resources...");
            
            UniTask delay2 = UniTask.Delay(750);
            UniTask updateVolumeTask = UpdateStructureVolume(structure);

            await UniTask.WhenAll(delay2, updateVolumeTask);
            
            MinecraftAssetProvider.ClearBlockStatePalette();
            await MinecraftAssetProvider.UpdateBlockStatePalette(structure.Palette);
            MinecraftAssetProvider.UpdateColorRules();
            
            LoaderCurtain.UpdateProgressLabel("Building...");
            UniTask delay3 = UniTask.Delay(1500);
            UniTask constructTask = Construct(structure);

            await UniTask.WhenAll(
                delay3.ContinueWith(() => 
                    LoaderCurtain.UpdateProgressLabel("Building a massive structure...")),
                constructTask);
            
            LoaderCurtain.UpdateProgressLabel("Done");
            
            //await CleanGarbageAsync();

            await UniTask.Delay(500);
            
            Root.SetActive(true);
            CameraController
                .Animate(
                    Root.GetComponent<Renderer>().bounds,
                    2f,
                    () => CameraController.InputDisabled = false)
                .Forget();
            LoaderCurtain.Hide();
        }

        private static async UniTask CleanGarbageAsync()
        {
            await UniTask.SwitchToMainThread();
            
            GC.Collect();
            await UniTask.Yield();
            GC.WaitForPendingFinalizers();
            await UniTask.Yield();
            GC.Collect();
            
            await UniTask.Yield();
        }
        
        private async UniTask<Structure> ParseNbtStructure(NbtFile nbtFile)
        {
            NbtList blocksTagList = nbtFile.RootTag.Get<NbtList>("blocks");
            NbtList paletteTagList = nbtFile.RootTag.Get<NbtList>("palette") 
                                     ?? nbtFile.RootTag.Get<NbtList>("palettes").Get<NbtList>(0);

            await UniTask.Yield();
            
            Structure structure = new()
            {
                Blocks = await ParseNbtBlocks(blocksTagList),
                Palette = ParseNbtPalette(paletteTagList)
            };

            return structure;
        }

        private async UniTask UpdateStructureVolume(Structure structure)
        {
            await UniTask.Yield();
            
            StructureVolume.Reset();

            float yieldTime = 0f;
            
            foreach (Block block in structure.Blocks)
            {
                StructureVolume[block.Pos] = CreateBlockState(structure.Palette[block.State]);
                
                if (yieldTime > 1f)
                {
                    await UniTask.Yield();
                    yieldTime = 0;
                }

                yieldTime += Time.deltaTime;
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
    
        private async UniTask<List<Block>> ParseNbtBlocks(NbtList blocksTagList)
        {
            List<Block> blocks = new List<Block>();

            float yieldTime = 0f;
            
            foreach (NbtTag nbtTag in blocksTagList)
            {
                var blockTag = (NbtCompound)nbtTag;

                Block block = new();
                Vector3 pos = new()
                {
                    x = blockTag["pos"][2].IntValue,
                    y = blockTag["pos"][1].IntValue,
                    z = blockTag["pos"][0].IntValue
                };
                block.Pos = pos;
                block.State = blockTag["state"].IntValue;
            
                blocks.Add(block);
                
                if (yieldTime > 1f)
                {
                    await UniTask.Yield();
                    yieldTime = 0;
                }

                yieldTime += Time.deltaTime;
            }

            return blocks;
        }

        private List<PaletteEntry> ParseNbtPalette(NbtList paletteTagList)
        {
            List<PaletteEntry> palette = new List<PaletteEntry>();

            foreach (NbtTag nbtTag in paletteTagList)
            {
                var paletteTag = (NbtCompound)nbtTag;

                PaletteEntry block = new();
            
                block.Name = paletteTag["Name"].StringValue.Replace("minecraft:", "");

                if (paletteTag.TryGet("Properties", out NbtTag result))
                {
                    var propertiesTag = (NbtCompound)result;

                    if (propertiesTag is not null)
                    {
                        foreach (NbtTag tag in propertiesTag.Tags)
                        {
                            string property = tag.Name;
                            string value = tag.StringValue;

                            block.Properties[property] = value;
                        } 
                    }
                }
            
                palette.Add(block);
            }
        
            return palette;
        }

        private static void ClearBlockVisual(GameObject modelObject)
        {
            if (modelObject.TryGetComponent<MeshFilter>(out var meshFilter))
            {
                meshFilter.sharedMesh = null;
            }
            
            foreach (Transform t in modelObject.transform)
            {
                Destroy(t.gameObject);
            }
        }
    
        private async UniTask Construct(Structure structure)
        {
            await UniTask.Yield();
            
            ClearBlockVisual(Root);
            
            MinecraftAssetProvider.ReleaseBlockStateHandles();
        
            List<UniTask> warmupTasks = new(structure.Palette.Count);

            float yieldTime = 0;

            foreach (PaletteEntry entry in structure.Palette)
            {
                warmupTasks.Add(MinecraftAssetProvider.WarmupBlockGeometriesAsync(entry.Name));
                
                if (yieldTime > 1f)
                {
                    await UniTask.Yield();
                    yieldTime = 0;
                }

                yieldTime += Time.deltaTime;
            }
            
            await UniTask.WhenAll(warmupTasks);
            
            foreach (Block block in structure.Blocks)
            {
                PaletteEntry paletteEntry = structure.Palette[block.State];

                if (paletteEntry.Name.Equals("air"))
                    continue;

                byte cullFlags = StructureVolume.GetCullFlagsFor(block.Pos);
            
                GameObject blockView = MinecraftAssetProvider.LoadModel(paletteEntry.Name, block.Pos, paletteEntry.Properties, cullFlags);
                Transform cachedTransform = blockView.transform;
                cachedTransform.parent = Root.transform;
                cachedTransform.position = block.Pos;

                if (yieldTime > 1f)
                {
                    await UniTask.Yield();
                    yieldTime = 0;
                }

                yieldTime += Time.deltaTime;
            }

            MeshCombiner meshCombiner = Root.AddComponent<MeshCombiner>();
            meshCombiner.CreateMultiMaterialMesh = true;
            meshCombiner.DestroyCombinedChildren = true;
            await meshCombiner.CombineMeshes(true);

            Renderer component = Root.GetComponent<Renderer>();
            CameraController.SetPivot(component.bounds.center);
        }
    
        private static bool IsFileNbtStructure(NbtFile file)
        {
            NbtTag sizeTag = file.RootTag.Get("size");
            NbtTag blocksTag = file.RootTag.Get("blocks");
            NbtTag paletteTag = file.RootTag.Get("palette");

            if (paletteTag is null)
            {
                NbtList palettesTag = file.RootTag.Get<NbtList>("palettes");
                paletteTag = palettesTag[0];
            }
        
            return sizeTag is not null 
                   && blocksTag is not null
                   && paletteTag is not null;
        }

        public record Structure
        {
            public List<Block> Blocks;
            public List<PaletteEntry> Palette;
        }

        public record PaletteEntry
        {
            public string Name;
            public readonly Dictionary<string, string> Properties = new();
        }
    
        public record Block
        {
            public Vector3 Pos;
            public int State;
            public string Nbt; //add nbt support
        }
    }
}