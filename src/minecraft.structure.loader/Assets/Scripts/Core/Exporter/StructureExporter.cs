using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityGLTF;

namespace Core.Exporter
{
    public interface IExportStrategy
    {
        byte[] GetMeshBytes(GameObject root);
    }
    
    public class GltfExporter : IExportStrategy
    {
        public byte[] GetMeshBytes(GameObject root)
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
    
    public class GlbExporter : IExportStrategy
    {
        public byte[] GetMeshBytes(GameObject root)
        {
            using MemoryStream stream = new();

            var exporter = new GLTFSceneExporter(
                new[] { root.transform },
                new ExportContext()
            );

            exporter.SaveGLBToStream(stream, string.Empty);

            return stream.ToArray();
        }
    }
    
    public class ObjExporter : IExportStrategy
    {
        public byte[] GetMeshBytes(GameObject root)
        {
            using var stream = new MemoryStream();

            var exporter = new GLTFSceneExporter(
                new[] { root.transform },
                new ExportContext()
            );

            exporter.SaveGLBToStream(stream, "test");

            return stream.ToArray();
        }
    }
    
    public static class StructureExporter
    {
        [DllImport("__Internal")]
        private static extern void DownloadFile(
            string filename,
            byte[] data,
            int length);
        
        public static void ExportToZip(string structureName, GameObject root, IExportStrategy exportStrategy)
        {
            using MemoryStream zipMemory = new();

            using (ZipArchive archive = new(zipMemory, ZipArchiveMode.Create, true))
            {
                ZipArchiveEntry modelEntry = archive.CreateEntry(
                    $"{structureName}.glb",
                    System.IO.Compression.CompressionLevel.Optimal);

                using (Stream entryStream = modelEntry.Open())
                {
                    byte[] modelBytes = exportStrategy.GetMeshBytes(root);

                    entryStream.Write(
                        modelBytes,
                        0,
                        modelBytes.Length);
                }

                ZipArchiveEntry pngEntry = archive.CreateEntry(
                    "atlas_texture.png",
                    System.IO.Compression.CompressionLevel.Optimal);

                using (Stream stream = pngEntry.Open())
                {
                    Material material = root.GetComponent<Renderer>().materials[0];
                    Texture2D atlas = material.mainTexture as Texture2D;
                    stream.Write(atlas.EncodeToPNG());
                }
            }

            byte[] zipBytes = zipMemory.ToArray();
            
#if UNITY_WEBGL && !UNITY_EDITOR
            DownloadFile(
                $"{structureName}.zip",
                zipBytes,
                zipBytes.Length);
#elif UNITY_EDITOR && UNITY_EDITOR_WIN
            File.WriteAllBytes(
                Path.Combine(Application.dataPath, $"../{structureName}.zip"), 
                zipBytes);
#endif
        }
    }
}