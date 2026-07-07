using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using CraftSharp;
using Core.AssetManagement;
using Core.Parser;
using Core.Parser.Model;
using Core.UI;
using Core.Utils;

namespace Core
{
    public class StructureBuilder : MonoBehaviour
    {
        public GameObject Root { get; private set; }
        public static StructureVolume StructureVolume;
    
        private CameraController _cameraController;
        private LoadingCurtain _loadingCurtain;

        private ParserRegistry _parserRegistry;
        
        public void InjectDependencies(
            LoadingCurtain loadingCurtain,
            CameraController cameraController)
        {
            _loadingCurtain = loadingCurtain;
            _cameraController = cameraController;
        }
        
        private void Awake()
        {
            Root = new GameObject("Structure");
            StructureVolume = new StructureVolume();
            _parserRegistry = new ParserRegistry();
        }
    
        public async UniTaskVoid BuildFor(byte[] fileContent, string fileType)
        {
            _cameraController.InputDisabled = true;
            Root.SetActive(false);
            _loadingCurtain.UpdateProgressLabel("Looking at your structure...");
            _loadingCurtain.Show();
            
            UniTask delay1 = UniTask.Delay(750);
            ParsedStructure structure = null;
            
            try
            {
                IParser parser = _parserRegistry.GetFor(fileType);
                UniTask<ParsedStructure> parseTask = parser.Parse(fileContent).Preserve();

                await UniTask.WhenAll(delay1, parseTask);
                structure = await parseTask;
            }
            catch (Exception e)
            {
                _loadingCurtain.UpdateProgressLabel("Can't parse your structure file");
                return;
            }
            
            _loadingCurtain.UpdateProgressLabel("Collecting resources...");
            
            UniTask delay2 = UniTask.Delay(750);
            UniTask updateVolumeTask = UpdateStructureVolume(structure);

            await UniTask.WhenAll(delay2, updateVolumeTask);
            
            MinecraftAssetProvider.ClearBlockStatePalette();
            await MinecraftAssetProvider.UpdateBlockStatePalette(structure.Palette);
            MinecraftAssetProvider.UpdateColorRules();
            
            _loadingCurtain.UpdateProgressLabel("Building...");
            UniTask delay3 = UniTask.Delay(1500);
            UniTask constructTask = Construct(structure);

            await UniTask.WhenAll(
                delay3.ContinueWith(() => 
                    _loadingCurtain.UpdateProgressLabel("Building a massive structure...")),
                constructTask);
            
            _loadingCurtain.UpdateProgressLabel("Done");

            await UniTask.Delay(500);
            
            Root.SetActive(true);
            _loadingCurtain.Hide();
            _cameraController
                .DoOrbitAnimation(
                    Root.GetComponent<Renderer>().bounds,
                    2f,
                    () => _cameraController.InputDisabled = false)
                .Forget();
        }

        private async UniTask UpdateStructureVolume(ParsedStructure structure)
        {
            await UniTask.Yield();
            
            StructureVolume.Reset();

            float yieldTime = 0f;
            
            foreach (BlockEntry block in structure.Blocks)
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
    
        private async UniTask Construct(ParsedStructure structure)
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
            
            foreach (BlockEntry block in structure.Blocks)
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
            await meshCombiner.CombineMeshes(false);

            Renderer component = Root.GetComponent<Renderer>();
            _cameraController.SetPivot(component.bounds.center);
        }
    }
}