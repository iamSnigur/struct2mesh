using System.Collections.Generic;

namespace Core.Parser.Model
{
    public record PaletteEntry
    {
        public string Name;
        public readonly Dictionary<string, string> Properties = new();
    }
}