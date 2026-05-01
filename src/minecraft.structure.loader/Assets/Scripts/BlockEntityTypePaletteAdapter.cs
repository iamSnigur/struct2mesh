using System;
using System.Collections.Generic;
using CraftSharp;
using UnityEngine;
using Object = UnityEngine.Object;

public class BlockEntityTypePaletteAdapter : BlockEntityTypePalette
{
    public static readonly BlockEntityTypePaletteAdapter Instance = new();

    private readonly Dictionary<ResourceLocation, BlockEntityType> _blockEntityMapping = new();

    public new bool GetBlockEntityForBlock(ResourceLocation blockId, out BlockEntityType blockEntityType)
    {
        bool found = _blockEntityMapping.TryGetValue(blockId, out BlockEntityType result);
        blockEntityType = result;
        return found;
    }

    protected override void ClearEntries()
    {
        base.ClearEntries();
        _blockEntityMapping.Clear();
    }

    public void PrepareData()
    {
        ClearEntries();

        try
        {
            TextAsset textAsset = Resources.Load<TextAsset>("block_entity_data");
            Json.JSONData blockEntityTypes = Json.ParseJson(textAsset.text);

            foreach ((string key, Json.JSONData blockEntityDef) in blockEntityTypes.Properties)
            {
                if (int.TryParse(blockEntityDef.Properties["protocol_id"].StringValue, out int numId))
                {
                    var blockEntityTypeId = ResourceLocation.FromString(key);
                    var associatedBlockIds = new HashSet<ResourceLocation>();
                    var newBlockEntityType = new BlockEntityType(blockEntityTypeId, associatedBlockIds);

                    foreach (var block in blockEntityDef.Properties["blocks"].DataArray)
                    {
                        var blockId = ResourceLocation.FromString(block.StringValue);

                        if (BlockStatePaletteAdapter.Instance.Check(blockId))
                        {
                            _blockEntityMapping.Add(blockId, newBlockEntityType);
                            associatedBlockIds.Add(blockId);
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"Block {blockId} which has block entity {blockEntityTypeId} is not present!");
                        }
                    }

                    AddEntry(blockEntityTypeId, numId, newBlockEntityType);
                }
                else
                {
                    Debug.LogWarning($"Invalid numeral block entity type key [{key}]");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading block entity types: {e.Message}");
        }
        finally
        {
            FreezeEntries();
        }
    }
}