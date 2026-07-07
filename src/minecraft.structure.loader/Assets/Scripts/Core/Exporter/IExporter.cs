using UnityEngine;

namespace Core.Exporter
{
    public interface IExporter
    {
        string Extension { get; }
        byte[] GetMeshBytes(GameObject root, string structureName);
    }
}