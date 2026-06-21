using System.IO;
using UnityEngine;
using UnityGLTF;

namespace Core.Exporter
{
    public class GlbExporter : IExportStrategy
    {
        public string Extension => "glb";

        public byte[] GetMeshBytes(GameObject root, string structureName)
        {
            using MemoryStream stream = new();

            GLTFSceneExporter exporter = new(
                new[] { root.transform },
                new ExportContext()
            );

            exporter.SaveGLBToStream(stream, structureName);

            return stream.ToArray();
        }
    }
}