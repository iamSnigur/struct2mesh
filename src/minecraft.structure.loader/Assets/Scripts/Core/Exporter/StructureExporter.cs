using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityGLTF;

namespace Core.Exporter
{
    public interface IExportStrategy
    {
        byte[] GetMeshBytes(GameObject structure);
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

            exporter.SaveGLBToStream(stream, "test");

            return stream.ToArray();
        }
        
        public static byte[] ExportGlb(GameObject root)
        {
            using var stream = new MemoryStream();

            var exporter = new GLTFSceneExporter(
                new[] { root.transform },
                new ExportContext()
            );

            exporter.SaveGLBToStream(stream, "model");

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
        
        public static void ExportToZip(GameObject structure, IExportStrategy exportStrategy)
        {
            using MemoryStream zipMemory = new();

            using (ZipArchive archive = new(zipMemory, ZipArchiveMode.Create, true))
            {
                ZipArchiveEntry modelEntry = archive.CreateEntry(
                    "model.glb",
                    System.IO.Compression.CompressionLevel.Optimal);

                using (Stream entryStream = modelEntry.Open())
                {
                    byte[] modelBytes = exportStrategy.GetMeshBytes(structure);

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
                    Material material = structure.GetComponent<Renderer>().materials[0];
                    Texture2D atlas = material.mainTexture as Texture2D;
                    stream.Write(atlas.EncodeToPNG());
                }
            }

            byte[] zipBytes = zipMemory.ToArray();
            
#if UNITY_WEBGL && !UNITY_EDITOR
            DownloadFile(
                "minecraft_structure.zip",
                zipBytes,
                zipBytes.Length);
#elif UNITY_EDITOR && UNITY_EDITOR_WIN
            File.WriteAllBytes(
                Path.Combine(Application.dataPath, "../model.zip"), 
                zipBytes);
#endif
        }
    }
}