using System;
using System.Collections.Generic;
using Core.AssetManagement.External;
using Core.Async;
using CraftSharp;
using CraftSharp.Resource;
using Cysharp.Threading.Tasks;
using Logic.Parser.Model;
using Logic.Utils;
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

        public static async UniTask Init()
        {
            if (Loaded)
                return;

            _world = new World();
            _resourcePackManager = ResourcePackManager.Instance;
            _blockStateLoader = new BlockStateLoader(_resourcePackManager);

            _colorRules = await AssetProvider.LoadByAddressAsync<TextAsset>(COLOR_RULES_ADDRESS);
            
            await PreloadResources();
        }

        private static void UpdateStatus(string status, string progress) =>
            Debug.Log($"{status}: {progress}");

        private static async UniTask PreloadResources()
        {
            try
            {
                await GatherResources(_resourcePackManager);
                await _resourcePackManager.LoadPacks_Mod(UpdateStatus);
                await Resources.UnloadUnusedAssets();
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

            if (!_resourcePackManager.BlockStateFileTable.TryGetValue(location, out string blockstateJson))
            {
                return;
            }

            RenderType renderType = BlockStateHelper.GetRenderTypeFor(blockStateId);
            OffsetType offsetType = BlockStateHelper.GetOffsetType(blockStateId);

            s_blockstateHandles.Add(await _blockStateLoader.LoadBlockStateModel(location, blockstateJson, renderType, offsetType));
        }
        
        public static BlockRenderData BuildBlock(ref Mesh.MeshData meshData, string blockStateId, Vector3 blockPos, Dictionary<string, string> prop, byte cullFlags)
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

            BlockRenderData blockRenderData = BlockMeshBuilder.BuildMeshData(ref meshData, currentState, blockPos, cullFlags, _world);
            return blockRenderData;
        }

        private static async UniTask LoadLocations(Dictionary<ResourceLocation, string> table, string manifestAddress)
        {
            TextAsset manifest = await AssetProvider.LoadByAddressAsync<TextAsset>(manifestAddress);
            var manifestIndexes = JsonConvert.DeserializeObject<Dictionary<string, string>>(manifest.text);

            table.Clear();

            LongTaskYielder yielder = new();
            
            foreach (var index in manifestIndexes)
            {
                ResourceLocation identifier = new(index.Key);
                table[identifier] = index.Value;
                
                await yielder.TryYieldAsync();
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

        public static async UniTask UpdateBlockStatePalette(List<PaletteEntry> structurePalette)
        {
            LongTaskYielder yielder = new();
            
            foreach (PaletteEntry paletteEntry in structurePalette)
            {
                BlockStatePaletteAdapter
                    .Instance
                    .RegisterBlockState(paletteEntry.Name, paletteEntry.Properties);

                await yielder.TryYieldAsync();
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