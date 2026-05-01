using System;
using System.Collections;
using System.Collections.Generic;
using CraftSharp;
using UnityEngine;
using fNbt;

public class NbtReader : MonoBehaviour
{
    public string StructureNbtFilePath;
    public TextAsset ColorRules;
    
    private GameObject _parent;
    public static StructureVolume StructureVolume;
    
    private void Awake()
    {
        Debug.Log(Application.persistentDataPath);
        _parent = new GameObject("Structure");
        StructureVolume = new StructureVolume();
    }

#if UNITY_EDITOR
    private IEnumerator Start()
    {
        while (!AssetProvider.Loaded)
            yield return null;

        LoadNbtStructure();
    }
#endif

    [ContextMenu("LoadNbtStructure")]
    public void LoadNbtStructure()
    {
        NbtFile nbtFile = new();
        nbtFile.LoadFromFile(StructureNbtFilePath);

        LoadStructure(nbtFile);
    }
    
    public void LoadStructure(NbtFile nbtFile)
    {
        if (!IsFileNbtStructure(nbtFile))
        {
            Debug.Log("File is not a nbt structure!");
            return;
        }

        Resources.UnloadUnusedAssets();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        Structure structure = ParseNbtStructure(nbtFile);
        UpdateStructureVolume(structure);
        AssetProvider.ClearBlockStatePalette();
        AssetProvider.UpdateBlockStatePalette(structure.Palette);
        AssetProvider.UpdateColorRules(ColorRules);
        Construct(structure);
    }
    
    private Structure ParseNbtStructure(NbtFile nbtFile)
    {
        NbtList blocksTagList = nbtFile.RootTag.Get<NbtList>("blocks");
        NbtList paletteTagList = nbtFile.RootTag.Get<NbtList>("palette");
        
        Structure structure = new()
        {
            Blocks = ParseNbtBlocks(blocksTagList),
            Palette = ParseNbtPalette(paletteTagList)
        };

        return structure;
    }

    private void UpdateStructureVolume(Structure structure)
    {
        StructureVolume.Reset();
        
        foreach (Block block in structure.Blocks) 
            StructureVolume[block.Pos] = CreateBlockState(structure.Palette[block.State]);
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
    
    private List<Block> ParseNbtBlocks(NbtList blocksTagList)
    {
        List<Block> blocks = new List<Block>();
        
        foreach (NbtTag nbtTag in blocksTagList)
        {
            var blockTag = (NbtCompound)nbtTag;

            Block block = new();
            Vector3 pos = new()
            {
                x = blockTag["pos"][2].IntValue,
                y = blockTag["pos"][1].IntValue,
                z = blockTag["pos"][0].IntValue
            };
            block.Pos = pos;
            block.State = blockTag["state"].IntValue;
            
            blocks.Add(block);
        }

        return blocks;
    }

    private List<PaletteEntry> ParseNbtPalette(NbtList paletteTagList)
    {
        List<PaletteEntry> palette = new List<PaletteEntry>();

        foreach (NbtTag nbtTag in paletteTagList)
        {
            var paletteTag = (NbtCompound)nbtTag;

            PaletteEntry block = new();
            
            block.Name = paletteTag["Name"].StringValue.Replace("minecraft:", "");

            if (paletteTag.TryGet("Properties", out NbtTag result))
            {
                var propertiesTag = (NbtCompound)result;

                if (propertiesTag is not null)
                {
                    foreach (NbtTag tag in propertiesTag.Tags)
                    {
                        string property = tag.Name;
                        string value = tag.StringValue;

                        block.Properties[property] = value;
                    } 
                }
            }
            
            palette.Add(block);
        }
        
        return palette;
    }

    private static void ClearBlockVisual(GameObject modelObject)
    {
        if (modelObject.TryGetComponent<MeshFilter>(out var meshFilter))
        {
            meshFilter.sharedMesh = null;
        }
            
        foreach (Transform t in modelObject.transform)
        {
            Destroy(t.gameObject);
        }
    }
    
    private void Construct(Structure structure)
    {
        ClearBlockVisual(_parent);

        foreach (Block block in structure.Blocks)
        {
            PaletteEntry paletteEntry = structure.Palette[block.State];

            if (paletteEntry.Name.Equals("air"))
                continue;

            byte cullFlags = StructureVolume.GetCullFlagsFor(block.Pos);
            
            GameObject blockView = AssetProvider.LoadModel(paletteEntry.Name, block.Pos, paletteEntry.Properties, cullFlags);
            Transform cachedTransform = blockView.transform;
            cachedTransform.parent = _parent.transform;
            cachedTransform.position = block.Pos;
        }

        MeshCombiner meshCombiner = _parent.AddComponent<MeshCombiner>();
        meshCombiner.CreateMultiMaterialMesh = true;
        //meshCombiner.DestroyCombinedChildren = true;
        //meshCombiner.CombineMeshes(true);

        Renderer component = _parent.GetComponent<Renderer>();
        FindFirstObjectByType<CameraController>().SetPivot(component.bounds.center);
    }
    
    private static bool IsFileNbtStructure(NbtFile file)
    {
        NbtTag sizeTag = file.RootTag.Get("size");
        NbtTag blocksTag = file.RootTag.Get("blocks");
        NbtTag paletteTag = file.RootTag.Get("palette"); // can be list of palettes, add support for them
        
        return sizeTag is not null 
               && blocksTag is not null
               && paletteTag is not null;
    }

    public record Structure
    {
        public List<Block> Blocks;
        public List<PaletteEntry> Palette;
    }

    public record PaletteEntry
    {
        public string Name;
        public readonly Dictionary<string, string> Properties = new();
    }
    
    public record Block
    {
        public Vector3 Pos;
        public int State;
        public string Nbt; //add nbt support
    }
}