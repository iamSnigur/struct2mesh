using System;
using System.Collections.Generic;
using Core.AssetManagement.External;
using CraftSharp;
using CraftSharp.Resource;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Core.AssetManagement
{
    public static class MinecraftAssetProvider
    {
        private const string TEXTURES_MANIFEST_ADDRESS = "textures_manifest_index";
        private const string MODELS_MANIFEST_ADDRESS = "models_manifest_index";
        private const string BLOCKSTATES_MANIFEST_ADDRESS = "blockstates_manifest_index";
        
        private static ResourcePackManager _resourcePackManager;
        private static BlockStateLoader _blockStateLoader;
        private static World _world;

        public static bool Loaded { get; private set; }

        public static async UniTaskVoid Init()
        {
            if (Loaded)
                return;

            _world = new World();
            _resourcePackManager = ResourcePackManager.Instance;
            _blockStateLoader = new BlockStateLoader(_resourcePackManager);

            BlockEntityPrefabs entityPrefabs = Resources.Load<BlockEntityPrefabs>("BlockEntity/Prefabs/BlockEntityPrefabs");
            BlockMeshBuilder.InjectDependencies(entityPrefabs);

            await GatherResources(_resourcePackManager);
            await PreloadResources();
        
            //await BedrockEntityResourceManager.Instance.LoadEntityResources(new DataLoadFlag(), (_) => {});
            //BedrockEntityResourceManager.Instance.LoadBlockEntityModelFolder("block_entity");
        }

        public static async UniTaskVoid WarmupAsync()
        {
            string key = "texture-source"; // "Assets/Resources_moved/vanilla/minecraft/textures/block"
            
            AsyncOperationHandle<IList<IResourceLocation>> handle = 
                Addressables.LoadResourceLocationsAsync(key, typeof(Texture2D));

            Debug.Log("Start location loading...");
            
            await handle.ToUniTask();
            
            Debug.Log($"Finished location loading with {handle.Result.Count} results");

            foreach (IResourceLocation location in handle.Result) 
                Debug.Log(location.PrimaryKey);
            
            Addressables.Release(handle);
        }

        private static void UpdateStatus(string status, string progress) =>
            Debug.Log($"{status}: {progress}");

        private static async UniTask PreloadResources()
        {
            try
            {
                await _resourcePackManager.LoadPacks_Mod(UpdateStatus);
                Loaded = true;
            }
            catch (Exception e)
            {
                Loaded = false;
                Debug.LogError($"Error loading resources: {e}");
                AssetProvider.Release();
            }
        }

        public static async UniTask WarmupBlockGeometriesAsync(string blockStateId)
        {
            ResourceLocation location = new(blockStateId);
            string blockstateJson = _resourcePackManager.BlockStateFileTable[location];

            RenderType renderType = BlockStateHelper.GetRenderTypeFor(blockStateId);
            OffsetType offsetType = BlockStateHelper.GetOffsetType(blockStateId);

            await _blockStateLoader.LoadBlockStateModel(location, blockstateJson, renderType, offsetType);
        }
        
        public static GameObject LoadModel(string blockStateId, Vector3 blockPos, Dictionary<string, string> prop, byte cullFlags)
        {
            ResourceLocation location = new(blockStateId);
            BlockState[] blockStates = BlockStatePaletteAdapter.Instance.GetAll(location);
            BlockStatePredicate predicate = new(prop);

            BlockState currentState = blockStates[0];

            foreach (BlockState state in blockStates)
            {
                if (predicate.Check(state))
                {
                    currentState = state;
                    break;
                }
            }

            GameObject gameObject = new(currentState.ToString());
            BlockMeshBuilder.BuildBlockGameObject(gameObject, currentState, blockPos, cullFlags, _world);

            return gameObject;
        }

        private static async UniTask LoadLocations(Dictionary<ResourceLocation, string> table, string manifestAddress)
        {
            TextAsset manifest = await AssetProvider.LoadByAddressAsync<TextAsset>(manifestAddress);
            var manifestIndexes = JsonConvert.DeserializeObject<Dictionary<string, string>>(manifest.text);

            table.Clear();
            
            foreach (var index in manifestIndexes)
            {
                ResourceLocation identifier = new(index.Key);
                table[identifier] = index.Value;
            }
        }
        
        private static async UniTask LoadTextures(ResourcePackManager manager) => 
            await LoadLocations(manager.TextureFileTable, TEXTURES_MANIFEST_ADDRESS);

        private static async UniTask LoadModels(ResourcePackManager manager) => 
            await LoadLocations(manager.BlockModelFileTable, MODELS_MANIFEST_ADDRESS);

        private static async UniTask LoadBlockstates(ResourcePackManager manager) => 
            await LoadLocations(manager.BlockStateFileTable, BLOCKSTATES_MANIFEST_ADDRESS);

        private static async UniTask GatherResources(ResourcePackManager manager)
        {
            UniTask texturesTask = LoadTextures(manager);
            UniTask modelsTask = LoadModels(manager);
            UniTask blockstatesTask = LoadBlockstates(manager);

            await UniTask.WhenAll(texturesTask, modelsTask, blockstatesTask);
            //AssetProvider.Release();
            //await Resources.UnloadUnusedAssets().ToUniTask();
        }

        public static void UpdateBlockStatePalette(List<NbtReader.PaletteEntry> structurePalette)
        {
            foreach (NbtReader.PaletteEntry paletteEntry in structurePalette)
            {
                BlockStatePaletteAdapter.Instance
                    .RegisterBlockState(paletteEntry.Name, paletteEntry.Properties);
            }

            BlockStatePaletteAdapter.Instance.InjectEntries();
        
            BlockEntityTypePaletteAdapter.Instance.PrepareData();
        }

        public static void UpdateColorRules(TextAsset colorRules) =>
            BlockStatePaletteAdapter.Instance.UpdateColorRules(colorRules);
    
        public static void ClearBlockStatePalette()
        {
            _resourcePackManager.StateModelTable.Clear();
            BlockStatePaletteAdapter.Instance.ClearInjectedEntries();
        }
    }
}