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

namespace Logic
{
    public class StructureBuilder : MonoBehaviour
    {
        public bool SeparateMeshes; // debug option
        
        public bool IsStructureLoaded { get; private set; }
        public GameObject Root { get; private set; }
        public static StructureVolume StructureVolume;
    
        private CameraController _cameraController;
        private LoadingCurtainView _loadingCurtainView;

        private ParserRegistry _parserRegistry;
        private IMeshGenerator _meshGenerator;
        private LongTaskYielder _taskYielder;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        
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
            UniTask constructTask = ConstructAsync(structure);

            await UniTask.WhenAll(
                delay3.ContinueWith(() => 
                    _loadingCurtainView.SetProgressText("Building a massive structure...")),
                constructTask);
            
            _loadingCurtainView.SetProgressText("Done");

            await UniTask.Delay(300);

            IsStructureLoaded = true;
            Root.SetActive(true);
            _loadingCurtainView.Hide();
            _cameraController
                .DoOrbitAnimationAsync(
                    _meshRenderer.bounds,
                    duration: 2f,
                    () => _cameraController.InputDisabled = false)
                .Forget();
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
        
            List<UniTask> warmupTasks = new(structure.Palette.Count);

            foreach (PaletteEntry entry in structure.Palette)
            {
                warmupTasks.Add(MinecraftAssetProvider.WarmupBlockGeometriesAsync(entry.Name));
                await _taskYielder.TryYieldAsync();
            }
            
            await UniTask.WhenAll(warmupTasks);

            await _meshGenerator.GenerateMesh(_meshFilter, _meshRenderer, structure, StructureVolume);
            
            _cameraController.SetPivot(_meshRenderer.bounds.center);
        }
    }
}