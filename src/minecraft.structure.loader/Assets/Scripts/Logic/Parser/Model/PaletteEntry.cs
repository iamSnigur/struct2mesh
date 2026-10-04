using System.Collections.Generic;

namespace Logic.Parser.Model
{
    public record PaletteEntry
    {
        public string Name;
        public readonly Dictionary<string, string> Properties = new();
    }
}