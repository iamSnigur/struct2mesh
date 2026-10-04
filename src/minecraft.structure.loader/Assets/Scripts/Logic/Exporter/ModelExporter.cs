using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;
using WebBridges;

namespace Logic.Exporter
{
    public static class ModelExporter
    {
        public static void ExportToZip(string structureName, GameObject root, IExporter exporter)
        {
            using MemoryStream zipMemory = new();

            using (ZipArchive archive = new(zipMemory, ZipArchiveMode.Create, true))
            {
                ZipArchiveEntry modelEntry = archive.CreateEntry(
                    $"{structureName}.{exporter.Extension}",
                    System.IO.Compression.CompressionLevel.Optimal);

                using (Stream entryStream = modelEntry.Open())
                {
                    byte[] modelBytes = exporter.GetMeshBytes(root, structureName);

                    entryStream.Write(
                        modelBytes,
                        0,
                        modelBytes.Length);
                }

                WriteTextures(archive, root.GetComponent<Renderer>());
            }

            byte[] zipBytes = zipMemory.ToArray();

            string archivePath = $"{exporter.Extension}_{structureName}.zip";
            
            ZipDownloadBridge.DownloadFile(archivePath, zipBytes, zipBytes.Length);
        }

        private static void WriteTextures(ZipArchive archive, Renderer renderer)
        {
            Texture2D[] textures = GetUniqueTextures(renderer);

            for (int i = 0; i < textures.Length; i++)
            {
                ZipArchiveEntry pngEntry = archive.CreateEntry(
                    $"{textures[i].name}.png",
                    System.IO.Compression.CompressionLevel.Optimal);

                using Stream stream = pngEntry.Open();
                stream.Write(textures[i].EncodeToPNG());
            }
        }
        
        private static Texture2D[] GetUniqueTextures(Renderer renderer)
        {
            Dictionary<int, Texture2D> unique = new();
            
            foreach (Material material in renderer.sharedMaterials)
            {
                Texture2D texture = material.mainTexture as Texture2D;
                
                if (texture is null)
                    continue;
                
                int id = texture.GetInstanceID();
                unique.TryAdd(id, texture);
            }

            return unique.Values.ToArray();
        }
    }
}