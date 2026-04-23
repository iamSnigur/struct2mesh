using System;
using System.Collections.Generic;
using CraftSharp;
using UnityEngine;

public class BlockEntityHelper : MonoBehaviour
{
    [SerializeField] private GameObject _defaultPrefab;
        
    [SerializeField] private GameObject _chestPrefab;
    [SerializeField] private GameObject _shulkerBoxPrefab;
    [SerializeField] private GameObject _bannerPrefab;
    [SerializeField] private GameObject _signPrefab;
    [SerializeField] private GameObject _hangingSignPrefab;
    
    private readonly Dictionary<ResourceLocation, GameObject> _blockEntityPrefabs = new();

    private void Start()
    {
        _blockEntityPrefabs.Clear();
        _blockEntityPrefabs.Add(BlockEntityType.CHEST_ID,             _chestPrefab);
        _blockEntityPrefabs.Add(BlockEntityType.ENDER_CHEST_ID,       _chestPrefab);
        _blockEntityPrefabs.Add(BlockEntityType.TRAPPED_CHEST_ID,     _chestPrefab);
        _blockEntityPrefabs.Add(BlockEntityType.SHULKER_BOX_ID,       _shulkerBoxPrefab);
        _blockEntityPrefabs.Add(BlockEntityType.BANNER_ID,            _bannerPrefab);
        _blockEntityPrefabs.Add(BlockEntityType.SIGN_ID,              _signPrefab);
        _blockEntityPrefabs.Add(BlockEntityType.HANGING_SIGN_ID,      _hangingSignPrefab);
    }

    private GameObject GetPrefabForType(ResourceLocation type) => 
        _blockEntityPrefabs.GetValueOrDefault(type, _defaultPrefab);

    /*public BlockEntityRender CreateBlockEntityRenderForItemModel(Transform parent, BlockState blockState, BlockEntityType blockEntityType)
    {
        GameObject blockEntityPrefab = GetPrefabForType(blockEntityType.TypeId);
        var blockEntityObj = Instantiate(blockEntityPrefab, parent);
        var blockEntityRender = blockEntityObj!.GetComponent<BlockEntityRender>();

        blockEntityObj.name = $"[Item Model] {blockEntityType}";
        blockEntityObj.transform.localPosition = Vector3.zero;
        blockEntityObj.transform.localEulerAngles = Vector3.zero;

        // Initialize the entity
        blockEntityRender.Initialize(null, blockState, blockEntityType, new());
                
        return blockEntityRender;
    }*/
}