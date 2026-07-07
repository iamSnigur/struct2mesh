using UnityEngine;

namespace Core.Parser.Model
{
    public record BlockEntry
    {
        public Vector3 Pos;
        public int State;
        public string Nbt; //add nbt support
    }
}