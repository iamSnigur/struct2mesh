using System;
using System.Collections.Generic;
using Core.AssetManagement.External;
using Core.Utils;
using CraftSharp;
using CraftSharp.Resource;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.AssetManagement
{
    public static class MinecraftAssetProvider
    {
        private const string TEXTURES_MANIFEST_ADDRESS = "textures_manifest_index";
        private const string MODELS_MANIFEST_ADDRESS = "models_manifest_index";
        private const string BLOCKSTATES_MANIFEST_ADDRESS = "blockstates_manifest_index";
        private const string COLOR_RULES_ADDRESS = "block_color_rules";
        
        private static ResourcePackManager _resourcePackManager;
        private static BlockStateLoader _blockStateLoader;
        private static World _world;
        private static TextAsset _colorRules;

        public static bool Loaded { get; private set; }

        public static async UniTaskVoid Init()
        {
            if (Loaded)
                return;

            _world = new World();
            _resourcePackManager = ResourcePackManager.Instance;
            _blockStateLoader = new BlockStateLoader(_resourcePackManager);

            _colorRules = await AssetProvider.LoadByAddressAsync<TextAsset>(COLOR_RULES_ADDRESS);
            
            await GatherResources(_resourcePackManager);
            await PreloadResources();
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

        private static readonly List<AssetHandle<TextAsset>> s_blockstateHandles = new();

        public static void ReleaseBlockStateHandles()
        {
            foreach (AssetHandle<TextAsset> handle in s_blockstateHandles) 
                handle.Release();
            
            s_blockstateHandles.Clear();
        }
        
        public static async UniTask WarmupBlockGeometriesAsync(string blockStateId)
        {
            ResourceLocation location = new(blockStateId);
            string blockstateJson = _resourcePackManager.BlockStateFileTable[location];

            RenderType renderType = BlockStateHelper.GetRenderTypeFor(blockStateId);
            OffsetType offsetType = BlockStateHelper.GetOffsetType(blockStateId);

            s_blockstateHandles.Add(await _blockStateLoader.LoadBlockStateModel(location, blockstateJson, renderType, offsetType));
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
        }

        public static async UniTask UpdateBlockStatePalette(List<NbtStructureParser.PaletteEntry> structurePalette)
        {
            await UniTask.Yield();
            
            float yieldTime = 0;
            
            foreach (NbtStructureParser.PaletteEntry paletteEntry in structurePalette)
            {
                BlockStatePaletteAdapter.Instance
                    .RegisterBlockState(paletteEntry.Name, paletteEntry.Properties);
                
                if (yieldTime > 0.5f)
                {
                    await UniTask.Yield();
                    yieldTime = 0;
                }

                yieldTime += Time.deltaTime;
            }

            BlockStatePaletteAdapter.Instance.InjectEntries();
        }

        public static void UpdateColorRules() => 
            BlockStatePaletteAdapter.Instance.UpdateColorRules(_colorRules);

        public static void ClearBlockStatePalette()
        {
            _resourcePackManager.StateModelTable.Clear();
            BlockStatePaletteAdapter.Instance.ClearInjectedEntries();
        }
    }
}