using System.Collections.Generic;
using UnityEngine;

namespace Logic.Parser.Model
{
    public record ParsedStructure
    {
        public Vector3 Size; 
        public List<BlockEntry> Blocks;
        public List<PaletteEntry> Palette;
    }
}