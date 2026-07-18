using System;
using System.Collections.Generic;
using Core.AssetManagement;
using Core.Async;
using CraftSharp;
using Cysharp.Threading.Tasks;
using Logic.Parser;
using Logic.Parser.Model;
using Logic.Utils;
using UI.Views;
using UnityEngine;

namespace Logic
{
    public class StructureBuilder : MonoBehaviour
    {
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
    
        private static void ClearRootVisual(GameObject modelObject)
        {
            if (modelObject.TryGetComponent<MeshFilter>(out var meshFilter))
            {
                Destroy(meshFilter.sharedMesh);
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
            
            ClearRootVisual(Root);
            
            MinecraftAssetProvider.ReleaseBlockStateHandles();
        
            List<UniTask> warmupTasks = new(structure.Palette.Count);

            LongTaskYielder yielder = new();
            
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
            await meshCombiner.CombineMeshes(false);

            Renderer component = Root.GetComponent<Renderer>();
            _cameraController.SetPivot(component.bounds.center);
        }
    }
}