using System;
using System.Collections.Generic;
using System.Linq;
using CraftSharp;
using Unity.Mathematics;
using UnityEngine;

public class BlockStatePaletteAdapter : BlockStatePalette
{
    public static readonly BlockStatePaletteAdapter Instance = new();

    private readonly Dictionary<string, List<BlockState>> _groupsRegistry = new();
    
    private readonly Dictionary<int, Func<World, BlockLoc, BlockState, int>> _blockColorRules = new();
    private const int ORIGINAL_BLOCK_COLOR = 0x00FFFFFF;

    public void RegisterBlockState(string blockStateId, Dictionary<string, string> properties)
    {
        ResourceLocation? fluidState = blockStateId switch
        {
            "water" => BlockState.WATER_ID,
            "lava" => BlockState.LAVA_ID,
            _ => null
        };

        BlockState newBlockState = new(
            new ResourceLocation(blockStateId),
            1,
            1,
            false,
            0b111111,
            true,
            false,
            15,
            0,
            fluidState,
            properties);

        if (!_groupsRegistry.TryGetValue(blockStateId, out _))
            _groupsRegistry[blockStateId] = new List<BlockState>();

        _groupsRegistry[blockStateId].Add(newBlockState);
    }

    public void ClearInjectedEntries()
    {
        ClearEntries();
        _groupsRegistry.Clear();
    }

    public void InjectEntries()
    {
        UnfreezeEntries();

        int stateId = 0;

        foreach (var group in _groupsRegistry)
        {
            int groupId = stateId;

            Dictionary<int, BlockState> blockStates = group.Value.ToDictionary(_ => stateId++);

            AddEntry(new ResourceLocation(group.Key), groupId, blockStates);
        }

        FreezeEntries();
    }

    public new int GetBlockColor(int stateId, World world, BlockLoc loc)
    {
        if (_blockColorRules.TryGetValue(stateId, out var rule))
            return rule.Invoke(world, loc, GetByNumId(stateId));
        
        return ORIGINAL_BLOCK_COLOR;
    }
    
    public void UpdateColorRules(TextAsset colorRulesAsset)
    {
        // Load block color rules...
        Json.JSONData colorRules = Json.ParseJson(colorRulesAsset.text);

        if (colorRules.Properties.TryGetValue("dynamic", out var dynamicRulesProperty))
        {
            foreach (var (ruleName, ruleValue) in dynamicRulesProperty.Properties)
            {
                Func<World, BlockLoc, BlockState, int> ruleFunc = ruleName switch
                {
                    "foliage" => (world, loc, _) => world.GetFoliageColor(loc),
                    "grass" => (world, loc, _) => world.GetGrassColor(loc),
                    "water" => (world, loc, _) => world.GetWaterColor(loc),
                    "redstone" => (_, _, state) => state.Properties
                        .TryGetValue("power", out var property)
                        ? math.clamp(math.clamp(int.Parse(property), 0, 15) * 17 + 38, 0, 255) << 16
                        : 38 << 16,

                    _ => (_, _, _) => 0
                };

                foreach (var blockId in ruleValue.DataArray
                             .Select(block => ResourceLocation.FromString(block.StringValue)))
                {
                    if (TryGetAllNumIds(blockId, out int[] stateIds))
                    {
                        foreach (var stateId in stateIds)
                        {
                            if (!_blockColorRules.TryAdd(stateId, ruleFunc))
                            {
                                Debug.LogWarning($"Failed to apply dynamic color rules to {blockId} ({stateId})!");
                            }
                        }
                    }
                }
            }
        }

        if (colorRules.Properties.TryGetValue("fixed", out var fixedRulesProperty))
        {
            foreach (var (key, ruleValue) in fixedRulesProperty.Properties)
            {
                var blockId = ResourceLocation.FromString(key);
                var colorVec = VectorUtil.Json2Float3(ruleValue);
                var fixedColor = ((int)math.clamp(math.round(colorVec.x), 0, 255) << 16) |
                                 ((int)math.clamp(math.round(colorVec.y), 0, 255) << 8) |
                                 (int)math.clamp(math.round(colorVec.z), 0, 255);
                int ruleFunc(World world, BlockLoc loc, BlockState state) => fixedColor;

                if (TryGetAllNumIds(blockId, out int[] stateIds))
                {
                    foreach (var stateId in stateIds)
                    {
                        if (!_blockColorRules.TryAdd(stateId, ruleFunc))
                        {
                            Debug.LogWarning($"Failed to apply fixed color rules to {blockId} ({stateId})!");
                        }
                    }
                }
            }
        }
    }
}