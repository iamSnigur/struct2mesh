#nullable enable
using System.Collections.Generic;
using System.Linq;
using CraftSharp.Resource;
using UnityEngine;

namespace CraftSharp
{
    public class MaterialManager : MonoBehaviour
    {
        [SerializeField] public Material? AtlasSolid;
        [SerializeField] public Material? AtlasCutout;
        [SerializeField] public Material? AtlasCutoutMipped;
        [SerializeField] public Material? AtlasTranslucent;

        [SerializeField] public Material? EntitySolid;
        [SerializeField] public Material? EntityCutout;
        [SerializeField] public Material? EntityCutoutDoubleSided;
        [SerializeField] public Material? EntityTranslucent;

        private readonly Dictionary<RenderType, Material> _atlasMaterials = new();
        private Dictionary<RenderType, Material> _foglessAtlasMaterials = new();
        private Material? _defaultAtlasMaterial;

        private bool _atlasInitialized = false;

        public Material GetAtlasMaterial(RenderType renderType, bool disableFog = false)
        {
            EnsureAtlasInitialized();
            if (disableFog)
            {
                return _foglessAtlasMaterials.GetValueOrDefault(renderType, _defaultAtlasMaterial!);
            }

            return _atlasMaterials.GetValueOrDefault(renderType, _defaultAtlasMaterial!);
        }

        public Material GetEntityMaterial(EntityRenderType renderType)
        {
            return renderType switch
            {
                EntityRenderType.SOLID              => EntitySolid!,
                EntityRenderType.CUTOUT             => EntityCutout!,
                EntityRenderType.CUTOUT_CULLOFF     => EntityCutoutDoubleSided!,
                EntityRenderType.TRANSLUCENT        => EntityTranslucent!,
                _                                   => EntitySolid!
            };
        }

        public void EnsureAtlasInitialized()
        {
            if (!_atlasInitialized) Initialize();
        }

        private void Initialize()
        {
            _atlasMaterials.Clear();
            var packManager = ResourcePackManager.Instance;

            // Solid
            var solid = new Material(AtlasSolid!);
            solid.SetTexture("_BaseMap", packManager.GetAtlasArray(false));
            _atlasMaterials.Add(RenderType.SOLID, solid);

            _defaultAtlasMaterial = solid;

            // Cutout & Cutout Mipped
            var cutout = new Material(AtlasCutout!);
            cutout.SetTexture("_BaseMap", packManager.GetAtlasArray(false));
            _atlasMaterials.Add(RenderType.CUTOUT, cutout);

            var cutoutMipped = new Material(AtlasCutoutMipped!);
            cutoutMipped.SetTexture("_BaseMap", packManager.GetAtlasArray(true));
            _atlasMaterials.Add(RenderType.CUTOUT_MIPPED, cutoutMipped);

            // Translucent
            var translucent = new Material(AtlasTranslucent!);
            translucent.SetTexture("_BaseMap", packManager.GetAtlasArray(false));
            _atlasMaterials.Add(RenderType.TRANSLUCENT, translucent);

            // Water, Foliage, Plants, Tall Plants
            _atlasMaterials.Add(RenderType.WATER, translucent);
            _atlasMaterials.Add(RenderType.FOLIAGE, cutout);
            _atlasMaterials.Add(RenderType.PLANTS, cutout);
            _atlasMaterials.Add(RenderType.TALL_PLANTS, cutout);

            // Make fogless variants
            _foglessAtlasMaterials = _atlasMaterials.ToDictionary(x => x.Key, x =>
            {
                // Make a copy of the material
                var m = new Material(x.Value);
                // Set disable fog keyword
                m.EnableKeyword("_DISABLE_FOG");
                return m;
            });

            _atlasInitialized = true;
        }
    }
}