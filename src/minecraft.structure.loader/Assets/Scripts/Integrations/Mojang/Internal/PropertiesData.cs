// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Global
namespace Integrations.Mojang.Internal
{
    internal record PropertiesData
    {
        public string profileName;
        public string profileId;
        public long timestamp;
        public TexturesData textures;

        internal record TexturesData
        {
            public TextureProxy SKIN;
            public TextureProxy CAPE;

            public record TextureProxy
            {
                public string url;
                public object metadata;
            }
        }
    }
}