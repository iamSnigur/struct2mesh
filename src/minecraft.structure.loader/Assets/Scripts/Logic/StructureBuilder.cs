using System;
using System.Collections.Generic;
using Core.AssetManagement;
using Core.Async;
using CraftSharp;
using Cysharp.Threading.Tasks;
using Logic.MeshGenerator;
using Logic.Parser;
using Logic.Parser.Model;
using Logic.Utils;
using UI.Views;
using UnityEngine;
using VContainer;

namespace Logic
{
    public class StructureBuilder : MonoBehaviour
    {
        public bool SeparateMeshes; // debug option
        
        public bool IsStructureLoaded { get; private set; }
        public GameObject Root { get; private set; }
        public static StructureVolume StructureVolume;
    
        private CameraController _cameraController;
        private ILoadingCurtain _loadingCurtain;

        private ParserRegistry _parserRegistry;
        private IMeshGenerator _meshGenerator;
        private LongTaskYielder _taskYielder;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        
        [Inject]
        public void InjectDependencies(
            ILoadingCurtain loadingCurtain,
            CameraController cameraController)
        {
            _loadingCurtain = loadingCurtain;
            _cameraController = cameraController;
        }
        
        private void Awake()
        {
            Root = new GameObject("Structure");
            _meshRenderer = Root.AddComponent<MeshRenderer>();
            _meshFilter = Root.AddComponent<MeshFilter>();
            _meshFilter.sharedMesh = new Mesh {name = "StructureMesh"};
            
            StructureVolume = new StructureVolume();
            _parserRegistry = new ParserRegistry();
            _meshGenerator = new SimpleMeshGenerator();
            _taskYielder = new LongTaskYielder();
        }

        private void OnDestroy()
        {
            ((IDisposable)_meshGenerator).Dispose();  
        }

        public async UniTaskVoid BuildFromBytesAsync(byte[] rawBytes, string fileType)
        {
            _cameraController.InputDisabled = true;
            IsStructureLoaded = false;
            Root.SetActive(false);
            
            _loadingCurtain.SetInfoText("Looking at your structure...");
            _loadingCurtain.SetProgressValue(0f);
            _loadingCurtain.Show();
            
            ParsedStructure structure = await ParseStructure(rawBytes, fileType);
            if (structure is null) return;

            _loadingCurtain.SetInfoText("Collecting resources...");
            _loadingCurtain.SetProgressValue(10f);
            
            await UpdateStructureVolumeAsync(structure).RunWithMinDuration(250);
            
            MinecraftAssetProvider.ClearBlockStatePalette();
            await MinecraftAssetProvider.UpdateBlockStatePaletteAsync(structure.Palette);
            MinecraftAssetProvider.UpdateColorRules();
            
            _loadingCurtain.SetInfoText("Building...");
            _loadingCurtain.SetProgressValue(25f);
            await ConstructAsync(structure).RunWithMinDuration(1000);
            
            _loadingCurtain.SetInfoText("Done");
            _loadingCurtain.SetProgressValue(100f);

            await UniTask.Delay(400);

            _cameraController.SetPivot(_meshRenderer.bounds.center);
            IsStructureLoaded = true;
            Root.SetActive(true);
            _loadingCurtain.SetProgressValue(0f);
            _loadingCurtain.Hide();
            _cameraController
                .DoOrbitAnimationAsync(
                    _meshRenderer.bounds,
                    duration: 2f,
                    () => _cameraController.InputDisabled = false)
                .Forget();
        }

        private async UniTask<ParsedStructure> ParseStructure(byte[] rawBytes, string fileType)
        {
            try
            {
                IParser parser = _parserRegistry.GetFor(fileType);
                ParserResult parserResult = await parser.Parse(rawBytes).Preserve()
                    .RunWithMinDuration(250);

                if (parserResult.IsSuccess) 
                    return parserResult.Structure;
                
                await ShowErrorAndHideCurtain(parserResult.Error);
                return null;

            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                await ShowErrorAndHideCurtain("Failed to parse structure");
                return null;
            }
        }

        private async UniTask ShowErrorAndHideCurtain(string message)
        {
            _loadingCurtain.SetErrorText(message);
            await UniTask.Delay(2000);
            _loadingCurtain.Hide();
        }

        private async UniTask UpdateStructureVolumeAsync(ParsedStructure structure)
        {
            await UniTask.Yield();
            
            StructureVolume.Reset();

            foreach (BlockEntry block in structure.Blocks)
            {
                StructureVolume[block.Pos] = CreateBlockState(structure.Palette[block.State]);
                await _taskYielder.TryYieldAsync();
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
                meshFilter.sharedMesh.Clear();
        }
    
        private async UniTask ConstructAsync(ParsedStructure structure)
        {
            ClearVisual(Root);
            
            MinecraftAssetProvider.ReleaseBlockStateHandles();
        
            _loadingCurtain.SetInfoText("Warming Up Block Geometries...");
            _loadingCurtain.SetProgressValue(40f);
            List<UniTask> warmupTasks = new(structure.Palette.Count);

            foreach (PaletteEntry entry in structure.Palette)
            {
                warmupTasks.Add(MinecraftAssetProvider.WarmupBlockGeometriesAsync(entry.Name));
                await _taskYielder.TryYieldAsync();
            }
            
            await UniTask.WhenAll(warmupTasks);
            
            _loadingCurtain.SetInfoText("Generating Mesh...");
            _loadingCurtain.SetProgressValue(75f);
            await _meshGenerator.GenerateMesh(_meshFilter, _meshRenderer, structure, StructureVolume);
        }
    }
}