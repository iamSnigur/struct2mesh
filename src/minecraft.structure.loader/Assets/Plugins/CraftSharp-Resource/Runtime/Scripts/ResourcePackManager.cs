#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Core.AssetManagement.External;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Mathematics;

namespace CraftSharp.Resource
{
    public class ResourcePackManager
    {
        public static readonly ResourceLocation BLANK_TEXTURE = new("builtin", "blank");
        public static readonly ResourceLocation EMPTY_TEXTURE = new("empty");
        public static readonly ResourceLocation FOLIAGE_COLORMAP = new("colormap/foliage");
        public static readonly ResourceLocation GRASS_COLORMAP = new("colormap/grass");

        private static Texture2D? blankTextureInstance;

        public static readonly ResourceLocation[] DESTROY_TEXTURES = new ResourceLocation[]
        {
            new("block/destroy_stage_0"), new("block/destroy_stage_1"), new("block/destroy_stage_2"),
            new("block/destroy_stage_3"), new("block/destroy_stage_4"), new("block/destroy_stage_5"),
            new("block/destroy_stage_6"), new("block/destroy_stage_7"), new("block/destroy_stage_8"),
            new("block/destroy_stage_9")
        };

        // Identifier -> Texture file path (For atlas)
        public readonly Dictionary<ResourceLocation, string> TextureFileTable = new();

        // Identifier -> Entity texture
        public readonly Dictionary<ResourceLocation, Texture2D> EntityTexture2DTable = new();

        // Identifier -> Block json model file path
        public readonly Dictionary<ResourceLocation, string> BlockModelFileTable = new();

        // Identifier -> Item json model file path
        public readonly Dictionary<ResourceLocation, string> ItemModelFileTable = new();

        // Identifier -> BlockState json model file path
        public readonly Dictionary<ResourceLocation, string> BlockStateFileTable = new();

        // Identifier -> Block model
        public readonly Dictionary<ResourceLocation, JsonModel> BlockModelTable = new();

        // Block state numeral id -> Block state geometries (One single block state may have a list of models to use randomly)
        public readonly Dictionary<int, BlockStateModel> StateModelTable = new();

        // Identifier -> Raw item model
        public readonly Dictionary<ResourceLocation, JsonModel> RawItemModelTable = new();

        // Item numeral id -> Item model
        public readonly Dictionary<int, ItemModel> ItemModelTable = new();

        public readonly HashSet<ResourceLocation> BuiltinEntityModels = new();

        public readonly HashSet<ResourceLocation> GeneratedItemModels = new();

        public readonly BlockModelLoader BlockModelLoader;
        public readonly BlockStateModelLoader StateModelLoader;

        public readonly ItemModelLoader ItemModelLoader;

        public int GeneratedItemModelPrecision { get; set; } = 16;
        public int GeneratedItemModelThickness { get; set; } = 1;

        // Identifier -> Particle json file path
        public readonly Dictionary<ResourceLocation, string> ParticleFileTable = new();

        public const int PARTICLE_ATLAS_SIZE = 1024;

        // Particle atlas
        private Texture2D? particleAtlas;

        // Particle type id -> meshes for its frames
        public readonly Dictionary<ResourceLocation, Mesh[]> ParticleMeshesTable = new();

        public bool Loaded { get; private set; } = false;

#nullable disable
        private Texture2D _blockAtlas;
        private Texture2D _blockEntityAtlas;
#nullable enable

        private Texture2DArray? destroyTextureArray;

        private readonly List<ResourcePack> packs = new();

        public static readonly ResourcePackManager Instance = new();

        private ResourcePackManager()
        {
            // Block model loaders
            BlockModelLoader = new BlockModelLoader(this);
            StateModelLoader = new BlockStateModelLoader(this);

            // Item model loader
            ItemModelLoader = new ItemModelLoader(this);
        }

        public void AddPack(ResourcePack pack) => packs.Add(pack);

        public void ClearPacks()
        {
            Loaded = false;

            // Clear up pack list
            packs.Clear();

            // Clear up loaded file tables
            TextureFileTable.Clear();
            BlockModelFileTable.Clear();
            ItemModelFileTable.Clear();
            BlockStateFileTable.Clear();
            ParticleFileTable.Clear();

            // Clear up loaded data tables
            BlockModelTable.Clear();
            StateModelTable.Clear();
            RawItemModelTable.Clear();
            ItemModelTable.Clear();
            BuiltinEntityModels.Clear();
            GeneratedItemModels.Clear();
            ParticleMeshesTable.Clear();
            EntityTexture2DTable.Clear();

            // And clear up colormap data
            World.ColormapSize = 0;
            World.GrassColormapPixels = new Color32[] { };
            World.FoliageColormapPixels = new Color32[] { };
        }

        private static string GetProgressString(int curCount, int totalCount)
        {
            return $" {Mathf.RoundToInt(curCount / (float)totalCount * 100)}%";
        }

        public async UniTask LoadPacksAsync_Mod(Action<string, string> updateStatus)
        {
            updateStatus("resource.info.gather_resource", string.Empty);
            foreach (var pack in packs) pack.GatherResources(this);

            updateStatus("resource.info.create_texture", string.Empty);
            await LoadAtlas();

            // Load block models...
            updateStatus("resource.info.load_block_model", string.Empty);

            foreach (ResourceLocation blockModelId in BlockModelFileTable.Keys)
            {
                try
                {
                    BlockModelLoader.LoadBlockModel(blockModelId);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load block model: {blockModelId}");
                    Debug.LogError(e);
                    throw;
                }
            }

            updateStatus("resource.info.resource_loaded", string.Empty);

            Loaded = true;
        }
        
        private readonly Dictionary<ResourceLocation, TextureInfo> _blockAtlasTable = new();
        private readonly Dictionary<ResourceLocation, TextureInfo> _blockEntityAtlasTable = new();

        /// <summary>
        /// Get texture uvs (x, y, depth in atlas array) and texture animation info (frame count, frame interval, frame size, frame per row)
        /// </summary>
        public (float3[] uvs, float4 anim) GetUVs(ResourceLocation identifier, Vector4 part, int areaRot)
        {
            var info = GetTextureInfo(identifier);
            if (info.frameCount > 1) // This texture is animated
            {
                var oneX = info.bounds.width / info.framePerRow; // Frame size on texture atlas array

                return (GetUVsAt(info.bounds, info.index, oneX, oneX, part, areaRot),
                    new(info.frameCount, info.frameInterval, oneX, info.framePerRow));
            }

            return (GetUVsAt(info.bounds, info.index, info.bounds.width, info.bounds.height, part, areaRot),
                float4.zero);
        }

        private static float3[] GetUVsAt(Rect bounds, int index, float oneU, float oneV, Vector4 part, int areaRot)
        {
            // Get texture offset in atlas
            float3 o = new(bounds.xMin, bounds.yMax - oneV, index + 0.1F);

            // vector:  x,  y,  z,  w
            // vector: x1, y1, x2, y2
            float u1 = part.x * oneU, v1 = part.y * oneV;
            float u2 = part.z * oneU, v2 = part.w * oneV;

            return areaRot switch
            {
                0 => new[]
                {
                    new float3(u1, oneV - v1, 0F) + o, new float3(u2, oneV - v1, 0F) + o,
                    new float3(u1, oneV - v2, 0F) + o, new float3(u2, oneV - v2, 0F) + o
                }, //   0 Deg
                1 => new[]
                {
                    new float3(v1, u1, 0F) + o, new float3(v1, u2, 0F) + o, new float3(v2, u1, 0F) + o,
                    new float3(v2, u2, 0F) + o
                }, //  90 Deg
                2 => new[]
                {
                    new float3(oneU - u1, v1, 0F) + o, new float3(oneU - u2, v1, 0F) + o,
                    new float3(oneU - u1, v2, 0F) + o, new float3(oneU - u2, v2, 0F) + o
                }, // 180 Deg
                3 => new[]
                {
                    new float3(oneV - v1, oneV - u1, 0F) + o, new float3(oneV - v1, oneU - u2, 0F) + o,
                    new float3(oneV - v2, oneU - u1, 0F) + o, new float3(oneV - v2, oneU - u2, 0F) + o
                }, // 270 Deg

                _ => new[]
                {
                    new float3(u1, oneV - v1, 0F) + o, new float3(u2, oneV - v1, 0F) + o,
                    new float3(u1, oneV - v2, 0F) + o, new float3(u2, oneV - v2, 0F) + o
                } // Default
            };
        }

        private TextureInfo GetTextureInfo(ResourceLocation identifier)
        {
            if (_blockAtlasTable.TryGetValue(identifier, out var info))
                return info;
            
            if (_blockEntityAtlasTable.TryGetValue(identifier, out info))
                return info;

            Debug.Log($"Texture {identifier} is not in atlas!");

            // Return missing no texture
            return _blockAtlasTable[ResourceLocation.INVALID];
        }

        public Texture2D GetBlockAtlas() => _blockAtlas;
        public Texture2D GetBlockEntityAtlas() => _blockEntityAtlas;

        public static Texture2D GetMissingEntityTexture(int width, int height)
        {
            Texture2D tex = new(width, height)
            {
                filterMode = FilterMode.Point
            };
            Color32 magenta = Color.magenta;
            Color32 black = Color.black;

            var colors = Enumerable.Repeat(magenta, width * height).ToArray();

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    if (((i >> 2) + (j >> 2)) % 2 == 0)
                        colors[i * width + j] = black;
                }
            }

            tex.SetPixels32(colors);
            // Update texture and mipmaps
            tex.Apply(true);

            return tex;
        }

        private async UniTask LoadAtlas()
        {
            // load two atlases
            _blockAtlasTable.Clear();
            await AtlasIndexHelper.LoadAsync(_blockAtlasTable, "block_atlas_index");
            _blockAtlas = await AssetProvider.LoadByAddressAsync<Texture2D>("block_atlas");
            
            _blockEntityAtlasTable.Clear();
            await AtlasIndexHelper.LoadAsync(_blockEntityAtlasTable, "block_entity_atlas_index");
            _blockEntityAtlas = await AssetProvider.LoadByAddressAsync<Texture2D>("block_entity_atlas");
        }
    }
}