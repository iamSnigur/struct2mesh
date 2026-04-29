using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

public class TextureExtractorTool : EditorWindow
{
    private bool _isRunning;
    
    [MenuItem("Tools/Minecraft Texture Extractor")]
    public static void ShowWindow()
    {
        TextureExtractorTool win = GetWindow<TextureExtractorTool>("MC Texture Extractor");
        win.minSize = new Vector2(250, 300);
        win.Show();
    }

    private void OnGUI()
    {
        GUI.enabled = !_isRunning;

        if (GUILayout.Button("Extract Textures", GUILayout.Height(30))) 
            StartExtraction(ExtractTextures);
        
        if (GUILayout.Button("Extract Json Models", GUILayout.Height(30))) 
            StartExtraction(ExtractModels);
    }

    private void StartExtraction(Action extractorCallback)
    {
        _isRunning = true;

        try
        {
            extractorCallback();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            AssetDatabase.SaveAssets();
            _isRunning = false;
        }
    }
    
    private void ExtractTextures()
    {
        string version = "26.1.2";
        string jarPath = @$"C:\Users\SNIGUR\AppData\Roaming\.minecraft\versions\{version}\{version}.jar";
        string mcPath = "assets/minecraft/textures/block";
        string savePath = "Assets/Resources/vanilla/minecraft/textures/block";
            
        string filter = mcPath.Replace('\\', '/').ToLowerInvariant();
 
        using (ZipArchive zip = ZipFile.OpenRead(jarPath))
        {
            List<ZipArchiveEntry> texturesInfos = 
                zip.Entries
                .Where(archiveEntry => IsAssetTexture(archiveEntry, filter) || IsAssetMcMeta(archiveEntry, filter))
                .ToList();
 
            if (texturesInfos.Count == 0)
            {
                Debug.Log("Nothing to export. Check your JAR path filter.");
                return;
            }
 
            // Prepare output root (absolute)
            string projectRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, ".."));
            
            string absOutputRoot = Path.GetFullPath(
                Path.Combine(projectRoot, savePath));

            foreach (ZipArchiveEntry entry in texturesInfos)
            {
                // Build destination path: strip the filter prefix so the Unity
                // folder mirrors whatever remains of the JAR path.
                string relativePath = entry.FullName.Replace('\\', '/').Replace(".mcmeta", ".json");
                
                if (!string.IsNullOrEmpty(filter) && relativePath.ToLowerInvariant().StartsWith(filter))
                    relativePath = relativePath[filter.Length..];
 
                // Safety: strip any leading slashes
                relativePath = relativePath.TrimStart('/', '\\');
 
                string destPath = Path.Combine(absOutputRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
 
                /*// Skip if exists and not overwriting
                if (!_overwriteExisting && File.Exists(destPath))
                {
                    _skippedFiles++;
                    continue;
                }*/
 
                string dir = Path.GetDirectoryName(destPath);
                
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using Stream src = entry.Open();
                using FileStream dst = File.Create(destPath);
                src.CopyTo(dst);
            }
        }
        
        ApplyImportSettings(absOutputRoot: Path.GetFullPath(
            Path.Combine(
                Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
                savePath)));
        
        
        Debug.Log("Textures extracted");
    }
    
    private void ExtractModels()
    {
        string version = "1.21.11";
        string jarPath = @$"C:\Users\SNIGUR\AppData\Roaming\.minecraft\versions\{version}\{version}.jar";
        string mcPath = "assets/minecraft/models/block";
        string savePath = "Assets/Resources/vanilla/minecraft/models/block";
            
        string filter = mcPath.Replace('\\', '/').ToLowerInvariant();
 
        using (ZipArchive zip = ZipFile.OpenRead(jarPath))
        {
            List<ZipArchiveEntry> jsonModelEntries = 
                zip.Entries
                .Where(archiveEntry => IsAssetJsonModel(archiveEntry, filter))
                .ToList();
 
            if (jsonModelEntries.Count == 0)
            {
                Debug.Log("Nothing to export. Check your JAR path filter.");
                return;
            }
 
            // Prepare output root (absolute)
            string projectRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, ".."));
            
            string absOutputRoot = Path.GetFullPath(
                Path.Combine(projectRoot, savePath));

            foreach (ZipArchiveEntry entry in jsonModelEntries)
            {
                // Build destination path: strip the filter prefix so the Unity
                // folder mirrors whatever remains of the JAR path.
                string relativePath = entry.FullName.Replace('\\', '/');
                if (!string.IsNullOrEmpty(filter) && relativePath.ToLowerInvariant().StartsWith(filter))
                    relativePath = relativePath[filter.Length..];
 
                // Safety: strip any leading slashes
                relativePath = relativePath.TrimStart('/', '\\');
 
                string destPath = Path.Combine(absOutputRoot, 
                    relativePath.Replace('/', Path.DirectorySeparatorChar));
 
                /*// Skip if exists and not overwriting
                if (!_overwriteExisting && File.Exists(destPath))
                {
                    _skippedFiles++;
                    continue;
                }*/
 
                string dir = Path.GetDirectoryName(destPath);
                
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using Stream src = entry.Open();
                using FileStream dst = File.Create(destPath);
                src.CopyTo(dst);
            }
        }
        
        AssetDatabase.Refresh();
        
        Debug.Log("Json models extracted");
    }

    private static bool IsAssetTexture(ZipArchiveEntry e, string filter)
    {
        return e.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
               && (string.IsNullOrEmpty(filter)
                   || e.FullName.Replace('\\', '/')
                       .ToLowerInvariant()
                       .StartsWith(filter));
    }
    
    private static bool IsAssetMcMeta(ZipArchiveEntry e, string filter)
    {
        return e.FullName.EndsWith(".mcmeta", StringComparison.OrdinalIgnoreCase)
               && (string.IsNullOrEmpty(filter)
                   || e.FullName.Replace('\\', '/')
                       .ToLowerInvariant()
                       .StartsWith(filter));
    }
    
    private static bool IsAssetJsonModel(ZipArchiveEntry e, string filter)
    {
        return e.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
               && (string.IsNullOrEmpty(filter)
                   || e.FullName.Replace('\\', '/')
                       .ToLowerInvariant()
                       .StartsWith(filter));
    }

    // ── Post-import settings ─────────────────────────────────────────────────
    private void ApplyImportSettings(string absOutputRoot)
    {
        string projectRoot = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..")) + Path.DirectorySeparatorChar;
 
        string[] files = Directory.GetFiles(absOutputRoot, "*.png",
            SearchOption.AllDirectories);
 
        AssetDatabase.Refresh();

        foreach (string f in files)
        {
            string assetPath = f.Replace('\\', '/')
                                .Replace(projectRoot.Replace('\\', '/'), "");
 
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            
            if (importer is null)
                continue;
 
            bool dirty = true;
 
            const TextureImporterType targetType = TextureImporterType.Sprite;

            if (importer.textureType != targetType)
            {
                importer.textureType = targetType;
                dirty = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                dirty = true;
            }

            importer.spriteImportMode = SpriteImportMode.Single;
            importer.isReadable = true;
            int maxTextureSize = GetTextureMaxSize(f);
            
            TextureImporterPlatformSettings platformSettings = new()
            {
                maxTextureSize = maxTextureSize,
                format = TextureImporterFormat.RGBA32,
                textureCompression = TextureImporterCompression.CompressedHQ,
                overridden = true
            };

            importer.SetPlatformTextureSettings(platformSettings);

            
            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }
    }

    private int GetTextureMaxSize(string path)
    {
        byte[] fileData = File.ReadAllBytes(path);
        Texture2D tex = new(2, 2);

        tex.LoadImage(fileData);

        int maxDim = Mathf.Max(tex.width, tex.height);

        DestroyImmediate(tex);

        int[] validSizes = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };

        foreach (int size in validSizes)
        {
            if (maxDim <= size)
                return size;
        }

        return 2048;
    }
    
    /// <summary>Returns the default OS-specific .minecraft versions folder.</summary>
    private static string GetDefaultMinecraftPath()
    {
#if UNITY_EDITOR_WIN
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ".minecraft", "versions");
#elif UNITY_EDITOR_OSX
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
            "Library", "Application Support", "minecraft", "versions");
#else // Linux
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
            ".minecraft", "versions");
#endif
    }
}