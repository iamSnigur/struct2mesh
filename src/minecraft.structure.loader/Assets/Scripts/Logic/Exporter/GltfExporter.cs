using System.IO;
using UnityEngine;
using UnityGLTF;

namespace Logic.Exporter
{
    public class GltfExporter : IExporter
    {
        public string Extension => "gltf";

        public byte[] GetMeshBytes(GameObject root, string structureName)
        {
            using var stream = new MemoryStream();

            var exporter = new GLTFSceneExporter(
                new[] { root.transform },
                new ExportContext()
            );

            //exporter.SaveGLTFandBin(); create adapter for this method and make it write to stream
            exporter.SaveGLBToStream(stream, string.Empty);

            return stream.ToArray();
        }
    }
}