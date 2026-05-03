using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CraftSharp;
using CraftSharp.Resource;
using CraftSharp.Resource.BedrockEntity;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class AssetProvider
{
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
        BedrockEntityResourceManager.Instance.LoadBlockEntityModelFolder("block_entity");
    }

    private static void UpdateStatus(string status, string progress) =>
        Debug.Log($"{status}: {progress}");

    private static async UniTask PreloadResources()
    {
        try
        {
            await _resourcePackManager.LoadPacks_Mod(new DataLoadFlag(), UpdateStatus);
            Loaded = true;
        }
        catch (Exception e)
        {
            Loaded = false;
            Debug.LogError($"Error loading resources: {e}");
        }
    }

    public static GameObject LoadModel(string blockStateId, Vector3 blockPos, Dictionary<string, string> prop, byte cullFlags)
    {
        ResourceLocation location = new(blockStateId);
        string blockstateJson = _resourcePackManager.BlockStateFileTable[location];

        RenderType renderType = BlockStateHelper.GetRenderTypeFor(blockStateId);
        OffsetType offsetType = BlockStateHelper.GetOffsetType(blockStateId);

        _blockStateLoader
            .LoadBlockStateModel(location, blockstateJson, renderType, offsetType);

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

    private static void LoadTextures(ResourcePackManager manager, string packPath, string subFolder)
    {
        string path = $"{packPath}/{subFolder}";

        foreach (Texture2D texture in Resources.LoadAll<Texture2D>(path))
        {
            ResourceLocation identifier = new(subFolder + '/' + texture.name);
            manager.TextureFileTable[identifier] = path + '/' + texture.name;
        }
    }

    private static void LoadModels(ResourcePackManager manager, string packPath, string subFolder)
    {
        string path = $"{packPath}/{subFolder}";

        foreach (TextAsset json in Resources.LoadAll<TextAsset>(path))
        {
            ResourceLocation identifier = new(subFolder + '/' + json.name);
            manager.BlockModelFileTable[identifier] = path + '/' + json.name;
        }
    }

    private static void LoadBlockstates(ResourcePackManager manager, string packPath, string subFolder)
    {
        string path = $"{packPath}/{subFolder}";

        foreach (TextAsset json in Resources.LoadAll<TextAsset>(path))
        {
            ResourceLocation identifier = new(json.name);
            manager.BlockStateFileTable[identifier] = path + json.name;
        }
    }

    private static async UniTask GatherResources(ResourcePackManager manager)
    {
        string texturesPath = "vanilla/minecraft/textures";
        LoadTextures(manager, texturesPath, "block");
        LoadTextures(manager, texturesPath, "entity");

        string modelsPath = "vanilla/minecraft/models";
        LoadModels(manager, modelsPath, "block");

        string blockstatesPath = "vanilla/minecraft/blockstates";
        LoadBlockstates(manager, blockstatesPath, "");

        await Resources.UnloadUnusedAssets().ToUniTask();
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