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
            
            _loadingCurtain.SetProgressText("Looking at your structure...");
            _loadingCurtain.SetProgressValue(0f);
            _loadingCurtain.Show();
            
            ParsedStructure structure;
            
            try
            {
                IParser parser = _parserRegistry.GetFor(fileType);
                structure = await parser.Parse(rawBytes).Preserve().RunWithMinDuration(250);
            }
            catch (ArgumentException e)
            {
                await ShowErrorAndHideCurtain(e, "Structure format is not supported");
                return;
            }
            catch (OverflowException e)
            {
                await ShowErrorAndHideCurtain(e, "Structure exceeded block limit");
                return;
            }
            
            _loadingCurtain.SetProgressText("Collecting resources...");
            _loadingCurtain.SetProgressValue(10f);
            
            await UpdateStructureVolumeAsync(structure).RunWithMinDuration(250);
            
            MinecraftAssetProvider.ClearBlockStatePalette();
            await MinecraftAssetProvider.UpdateBlockStatePaletteAsync(structure.Palette);
            MinecraftAssetProvider.UpdateColorRules();
            
            _loadingCurtain.SetProgressText("Building...");
            _loadingCurtain.SetProgressValue(25f);
            await ConstructAsync(structure).RunWithMinDuration(1000);
            
            _loadingCurtain.SetProgressText("Done");
            _loadingCurtain.SetProgressValue(100f);

            await UniTask.Delay(400);

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

        private async UniTask ShowErrorAndHideCurtain(Exception e, string message)
        {
            Debug.LogError(e);
            _loadingCurtain.SetProgressText(message);
            await UniTask.Delay(1500);
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
        
            _loadingCurtain.SetProgressText("Warming Up Block Geometries...");
            _loadingCurtain.SetProgressValue(60f);
            List<UniTask> warmupTasks = new(structure.Palette.Count);

            foreach (PaletteEntry entry in structure.Palette)
            {
                warmupTasks.Add(MinecraftAssetProvider.WarmupBlockGeometriesAsync(entry.Name));
                await _taskYielder.TryYieldAsync();
            }
            
            await UniTask.WhenAll(warmupTasks);
            
            _loadingCurtain.SetProgressText("Generating Mesh...");
            _loadingCurtain.SetProgressValue(90f);
            await _meshGenerator.GenerateMesh(_meshFilter, _meshRenderer, structure, StructureVolume);
            
            _cameraController.SetPivot(_meshRenderer.bounds.center);
        }
    }
}