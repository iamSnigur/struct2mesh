using System.Collections.Generic;

namespace Logic.Parser.Model
{
    public record ParsedStructure
    {
        public List<BlockEntry> Blocks;
        public List<PaletteEntry> Palette;
    }
}