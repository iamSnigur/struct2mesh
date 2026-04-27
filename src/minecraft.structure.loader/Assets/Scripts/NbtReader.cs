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
    private StructureVolume _structureVolume;
    
    private void Awake()
    {
        _parent = new GameObject("Structure");
        _structureVolume = new StructureVolume();
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

    private class StructureVolume
    {
        private const byte CULL_NONE = 0b111111;
        private const byte CULL_UP = 0b111110;
        private const byte CULL_DOWN = 0b111101;
        private const byte CULL_FORWARD = 0b101111;
        private const byte CULL_BACK = 0b011111;
        private const byte CULL_LEFT = 0b110111;
        private const byte CULL_RIGHT = 0b111011;
        
        private readonly Dictionary<Vector3, string> _volumeData = new();

        public string this[Vector3 index]
        {
            get => _volumeData[index];
            set => _volumeData[index] = value;
        }

        public void Reset() => 
            _volumeData.Clear();

        public byte GetCullFlagsFor(Vector3 pos)
        {
            if (!IsBlockSolidAt(pos))
                return CULL_NONE;
            
            byte cullFlags = CULL_NONE;

            if (IsBlockSolidAt(pos.Up()))
                cullFlags &= CULL_UP;
            
            if (IsBlockSolidAt(pos.Down()))
                cullFlags &= CULL_DOWN;
            
            if (IsBlockSolidAt(pos.Left()))
                cullFlags &= CULL_LEFT;
            
            if (IsBlockSolidAt(pos.Right()))
                cullFlags &= CULL_RIGHT;
            
            if (IsBlockSolidAt(pos.Forward()))
                cullFlags &= CULL_FORWARD;
            
            if (IsBlockSolidAt(pos.Back()))
                cullFlags &= CULL_BACK;
            
            return cullFlags;
        }

        private bool IsBlockSolidAt(Vector3 pos)
        {
            if (!_volumeData.TryGetValue(pos, out string blockStateId))
                return false;

            RenderType renderType = BlockStateHelper.GetRenderTypeFor(blockStateId);
            
            if (renderType is not RenderType.SOLID)
                return false;
            
            return !BlockStateHelper.IsSemiSolid(blockStateId);
        }
    }

    private void UpdateStructureVolume(Structure structure)
    {
        _structureVolume.Reset();
        
        foreach (Block block in structure.Blocks) 
            _structureVolume[block.Pos] = structure.Palette[block.State].Name;
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

            byte cullFlags = _structureVolume.GetCullFlagsFor(block.Pos);
            
            GameObject blockView = AssetProvider.LoadModel(paletteEntry.Name, paletteEntry.Properties, cullFlags);
            Transform cachedTransform = blockView.transform;
            cachedTransform.parent = _parent.transform;
            cachedTransform.position = block.Pos;
        }

        MeshCombiner meshCombiner = _parent.AddComponent<MeshCombiner>();
        meshCombiner.CreateMultiMaterialMesh = true;
        meshCombiner.CombineMeshes(true);

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
    }
}