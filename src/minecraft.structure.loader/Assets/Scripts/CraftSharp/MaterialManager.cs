using System.Collections.Generic;
using CraftSharp.Resource;
using Logic.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace CraftSharp
{
    public class MaterialManager : MonoBehaviour
    {
        private static readonly int s_baseMap = Shader.PropertyToID("_BaseMap");
        
        [SerializeField] public Material AtlasSolid;
        [SerializeField] public Material AtlasCutout;
        [SerializeField] public Material AtlasTranslucent;

        private readonly Dictionary<RenderType, Material> _blockAtlasMaterials = new();
        private readonly Dictionary<RenderType, Material> _blockEntityAtlasMaterials = new();

        public void Initialize(ResourcePackManager packManager)
        {
            InitBlockAtlasMaterials(packManager);
            InitBlockEntityAtlasMaterials(packManager);
        }

        public Material GetAtlasMaterial(RenderType renderType, ResourceLocation blockId)
        {
            return BlockEntityHelper.IsBlockEntity(blockId.Path) 
                ? GetBlockEntityMaterial(renderType) 
                : GetBlockMaterial(renderType);
        }

        public Material GetBlockMaterial(RenderType renderType)
        {
            _blockAtlasMaterials.TryGetValue(renderType, out Material material);
            Assert.IsNotNull(
                material,
                $"Material not exists for render type {renderType} in block table");

            return material;
        }

        public Material GetBlockEntityMaterial(RenderType renderType)
        {
            _blockEntityAtlasMaterials.TryGetValue(renderType, out Material material);
            Assert.IsNotNull(
                material,
                $"Material not exists for render type {renderType} in block entity table");

            return material;
        }

        private void InitBlockAtlasMaterials(ResourcePackManager packManager)
        {
            _blockAtlasMaterials.Clear();

            // Solid
            var solid = new Material(AtlasSolid!);
            
            solid.SetTexture(s_baseMap, packManager.GetBlockAtlas());
            _blockAtlasMaterials.Add(RenderType.SOLID, solid);

            // Cutout & Cutout Mipped
            var cutout = new Material(AtlasCutout!);
            cutout.SetTexture(s_baseMap, packManager.GetBlockAtlas());
            _blockAtlasMaterials.Add(RenderType.CUTOUT, cutout);

            // Translucent
            var translucent = new Material(AtlasTranslucent!);
            translucent.SetTexture(s_baseMap, packManager.GetBlockAtlas());
            _blockAtlasMaterials.Add(RenderType.TRANSLUCENT, translucent);

            // Water, Foliage, Plants, Tall Plants
            _blockAtlasMaterials.Add(RenderType.WATER, translucent);
            _blockAtlasMaterials.Add(RenderType.FOLIAGE, cutout);
            _blockAtlasMaterials.Add(RenderType.PLANTS, cutout);
            _blockAtlasMaterials.Add(RenderType.TALL_PLANTS, cutout);
        }
        
        private void InitBlockEntityAtlasMaterials(ResourcePackManager packManager)
        {
            _blockEntityAtlasMaterials.Clear();

            // Solid
            var solid = new Material(AtlasSolid!);
            solid.SetTexture(s_baseMap, packManager.GetBlockEntityAtlas());
            _blockEntityAtlasMaterials.Add(RenderType.SOLID, solid);

            // Cutout & Cutout Mipped
            var cutout = new Material(AtlasCutout!);
            cutout.SetTexture(s_baseMap, packManager.GetBlockEntityAtlas());
            _blockEntityAtlasMaterials.Add(RenderType.CUTOUT, cutout);

            // Translucent
            var translucent = new Material(AtlasTranslucent!);
            translucent.SetTexture(s_baseMap, packManager.GetBlockEntityAtlas());
            _blockEntityAtlasMaterials.Add(RenderType.TRANSLUCENT, translucent);
        }
    }
}