using UnityEngine;

namespace Core.Exporter
{
    public interface IExportStrategy
    {
        string Extension { get; }
        byte[] GetMeshBytes(GameObject root, string structureName);
    }
}