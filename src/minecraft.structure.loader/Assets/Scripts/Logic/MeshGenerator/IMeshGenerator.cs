using Cysharp.Threading.Tasks;
using Logic.Parser.Model;
using UnityEngine;

namespace Logic.MeshGenerator
{
    public interface IMeshGenerator
    {
        UniTask GenerateMesh(MeshFilter meshFilter, MeshRenderer meshRenderer, ParsedStructure structure, StructureVolume structureVolume);
    }
}