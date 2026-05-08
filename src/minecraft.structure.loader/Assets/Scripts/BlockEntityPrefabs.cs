using System.Collections.Generic;
using CraftSharp;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockEntityPrefabs", menuName = "BlockEntity/BlockEntityPrefabs")]
public class BlockEntityPrefabs : ScriptableObject
{
    public GameObject DefaultPrefab;
        
    public GameObject ChestPrefab;
    public GameObject ShulkerBoxPrefab;
    public GameObject BannerPrefab;
    public GameObject SignPrefab;
    public GameObject HangingSignPrefab;
    // more prefabs
        
    private readonly Dictionary<ResourceLocation, GameObject> _blockEntityPrefabs = new();

    private void OnEnable()
    {
        _blockEntityPrefabs.Clear();
            
        _blockEntityPrefabs.Add(BlockEntityType.CHEST_ID,             ChestPrefab);
        _blockEntityPrefabs.Add(BlockEntityType.ENDER_CHEST_ID,       ChestPrefab);
        _blockEntityPrefabs.Add(BlockEntityType.TRAPPED_CHEST_ID,     ChestPrefab);
        _blockEntityPrefabs.Add(new ResourceLocation("copper_chest"),     ChestPrefab);
        _blockEntityPrefabs.Add(new ResourceLocation("waxed_copper_chest"),     ChestPrefab);
        _blockEntityPrefabs.Add(BlockEntityType.SHULKER_BOX_ID,       ShulkerBoxPrefab);
        _blockEntityPrefabs.Add(BlockEntityType.BANNER_ID,            BannerPrefab);
        _blockEntityPrefabs.Add(BlockEntityType.SIGN_ID,              SignPrefab);
        _blockEntityPrefabs.Add(BlockEntityType.HANGING_SIGN_ID,      HangingSignPrefab);
    }

    public GameObject GetPrefabForType(ResourceLocation type) => 
        _blockEntityPrefabs.GetValueOrDefault(type, DefaultPrefab);
}