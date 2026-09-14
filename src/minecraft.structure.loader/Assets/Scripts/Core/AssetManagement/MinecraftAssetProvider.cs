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
using Object = UnityEngine.Object;

namespace Core.AssetManagement
{
    public static class MinecraftAssetProvider
    {
        private const string COLOR_RULES_ADDRESS = "block_color_rules";
        
        private static ResourcePackManager s_resourcePackManager;
        private static BlockStateLoader s_blockStateLoader;
        private static World s_world;
        private static TextAsset s_colorRules;
        
        private static readonly List<AssetHandle<TextAsset>> s_blockstateHandles = new();

        public static bool Loaded { get; private set; }

        public static async UniTask Init()
        {
            if (Loaded)
                return;

            s_world = new World();
            s_resourcePackManager = ResourcePackManager.Instance;
            s_blockStateLoader = new BlockStateLoader(s_resourcePackManager);
            s_colorRules = await AssetProvider.LoadByAddressAsync<TextAsset>(COLOR_RULES_ADDRESS);

            await PreloadResourcesAsync();
            
            MaterialManager materialManager = Object.FindFirstObjectByType<MaterialManager>();
            materialManager.Initialize(s_resourcePackManager);
            BlockMeshBuilder.InjectDependencies(materialManager);
        }

        public static void Release()
        {
            ReleaseBlockStateHandles();
            AssetProvider.Release();
        }

        private static void UpdateStatus(string status, string progress) =>
            Debug.Log($"{status}: {progress}");

        private static async UniTask PreloadResourcesAsync()
        {
            try
            {
                await GatherResourcesAsync(s_resourcePackManager);
                await s_resourcePackManager.LoadPacksAsync(UpdateStatus);
                await Resources.UnloadUnusedAssets();
                Loaded = true;
            }
            catch (Exception e)
            {
                Loaded = false;
                Debug.LogError($"Error loading resources: {e}");
                Release();
            }
        }

        public static void ReleaseBlockStateHandles()
        {
            foreach (AssetHandle<TextAsset> handle in s_blockstateHandles) 
                handle.Release();
            
            s_blockstateHandles.Clear();
        }
        
        public static async UniTask WarmupBlockGeometriesAsync(string blockStateId)
        {
            ResourceLocation location = new(blockStateId);

            if (!BlockStateExists(location, out string blockstateJson))
                return;

            RenderType renderType = BlockStateHelper.GetRenderTypeFor(blockStateId);
            OffsetType offsetType = BlockStateHelper.GetOffsetType(blockStateId);

            s_blockstateHandles
                .Add(await s_blockStateLoader.LoadBlockStateModel(location, blockstateJson, renderType, offsetType));
        }

        private static bool BlockStateExists(ResourceLocation location, out string blockstateJson)
        {
            return s_resourcePackManager
                .BlockStateFileTable
                .TryGetValue(location, out blockstateJson);
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

            BlockRenderData blockRenderData = BlockMeshBuilder.BuildMeshData(ref meshData, currentState, blockPos, cullFlags, s_world);
            return blockRenderData;
        }

        private static async UniTask LoadLocationsAsync(
            Dictionary<ResourceLocation, string> table,
            string indexAddress)
        {
            TextAsset index = await AssetProvider.LoadByAddressAsync<TextAsset>(indexAddress);
            var manifestIndexes = JsonConvert.DeserializeObject<Dictionary<string, string>>(index.text);

            table.Clear();

            LongTaskYielder yielder = new();
            
            foreach (var indexPair in manifestIndexes)
            {
                ResourceLocation identifier = new(indexPair.Key);
                table[identifier] = indexPair.Value;
                
                await yielder.TryYieldAsync();
            }
        }
        
        private static async UniTask LoadModelsAsync(ResourcePackManager manager) => 
            await LoadLocationsAsync(manager.BlockModelFileTable, Constants.MODELS_INDEX);

        private static async UniTask LoadBlockstatesAsync(ResourcePackManager manager) => 
            await LoadLocationsAsync(manager.BlockStateFileTable, Constants.BLOCKSTATE_INDEX);

        private static async UniTask GatherResourcesAsync(ResourcePackManager manager)
        {
            UniTask modelsTask = LoadModelsAsync(manager);
            UniTask blockstatesTask = LoadBlockstatesAsync(manager);

            await UniTask.WhenAll(modelsTask, blockstatesTask);
        }

        public static async UniTask UpdateBlockStatePaletteAsync(List<PaletteEntry> structurePalette)
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
            BlockStatePaletteAdapter.Instance.UpdateColorRules(s_colorRules);

        public static void ClearBlockStatePalette()
        {
            s_resourcePackManager.StateModelTable.Clear();
            BlockStatePaletteAdapter.Instance.ClearInjectedEntries();
        }
    }
}