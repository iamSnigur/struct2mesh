using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Core.Exporter
{
    public static class ModelExporter
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
                    $"{structureName}.{exportStrategy.Extension}",
                    System.IO.Compression.CompressionLevel.Optimal);

                using (Stream entryStream = modelEntry.Open())
                {
                    byte[] modelBytes = exportStrategy.GetMeshBytes(root, structureName);

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
                $"{exportStrategy.Extension}_{structureName}.zip",
                zipBytes,
                zipBytes.Length);
#elif UNITY_EDITOR && UNITY_EDITOR_WIN
            File.WriteAllBytes(
                Path.Combine(Application.dataPath, $"../{exportStrategy.Extension}_{structureName}.zip"), 
                zipBytes);
#endif
        }
    }
}