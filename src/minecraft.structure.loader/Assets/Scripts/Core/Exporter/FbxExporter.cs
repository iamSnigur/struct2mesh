using UnityEngine;

namespace Core.Exporter
{
    public class FbxExporter : IExportStrategy
    {
        public string Extension => "fbx";

        public byte[] GetMeshBytes(GameObject root, string structureName)
        {
            Debug.Log("Not implemented");
            return null;
        }
    }
}