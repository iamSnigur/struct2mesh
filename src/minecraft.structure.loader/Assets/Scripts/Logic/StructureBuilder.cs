using System;
using System.Collections.Generic;
using Core.AssetManagement;
using Core.Async;
using CraftSharp;
using Cysharp.Threading.Tasks;
using Logic.Parser;
using Logic.Parser.Model;
using Logic.Utils;
using UI;
using UnityEngine;

namespace Logic
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
    
        public async UniTaskVoid BuildFromBytesAsync(byte[] rawBytes, string fileType)
        {
            _cameraController.InputDisabled = true;
            Root.SetActive(false);
            _loadingCurtain.SetProgressText("Looking at your structure...");
            _loadingCurtain.Show();
            
            UniTask delay1 = UniTask.Delay(750);
            ParsedStructure structure = null;
            
            try
            {
                IParser parser = _parserRegistry.GetFor(fileType);
                UniTask<ParsedStructure> parseTask = parser.Parse(rawBytes).Preserve();

                await UniTask.WhenAll(delay1, parseTask);
                structure = await parseTask;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                _loadingCurtain.SetProgressText("Can't parse your structure file");
                await UniTask.Delay(500);
                _loadingCurtain.Hide();
                return;
            }
            
            _loadingCurtain.SetProgressText("Collecting resources...");
            
            UniTask delay2 = UniTask.Delay(750);
            UniTask updateVolumeTask = UpdateStructureVolumeAsync(structure);

            await UniTask.WhenAll(delay2, updateVolumeTask);
            
            MinecraftAssetProvider.ClearBlockStatePalette();
            await MinecraftAssetProvider.UpdateBlockStatePalette(structure.Palette);
            MinecraftAssetProvider.UpdateColorRules();
            
            _loadingCurtain.SetProgressText("Building...");
            UniTask delay3 = UniTask.Delay(1500);
            UniTask constructTask = Construct(structure);

            await UniTask.WhenAll(
                delay3.ContinueWith(() => 
                    _loadingCurtain.SetProgressText("Building a massive structure...")),
                constructTask);
            
            _loadingCurtain.SetProgressText("Done");

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

        private async UniTask UpdateStructureVolumeAsync(ParsedStructure structure)
        {
            await UniTask.Yield();
            
            StructureVolume.Reset();

            LongTaskYielder yielder = new(2.5f);
            
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

            LongTaskYielder yielder = new(2.5f);
            
            foreach (PaletteEntry entry in structure.Palette)
            {
                warmupTasks.Add(MinecraftAssetProvider.WarmupBlockGeometriesAsync(entry.Name));

                await yielder.TryYieldAsync();
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

                await yielder.TryYieldAsync();
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