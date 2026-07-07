using System.Collections.Generic;

namespace Core.Parser.Model
{
    public record ParsedStructure
    {
        public List<BlockEntry> Blocks;
        public List<PaletteEntry> Palette;
    }
}