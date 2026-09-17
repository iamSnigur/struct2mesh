// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Global
namespace Integrations.Mojang.Internal
{
    internal record SkinProperties
    {
        public string name;
        public string id;
        public Property[] properties;

        internal record Property
        {
            public string name;
            public string value; // PropertiesData
        }
    }
}