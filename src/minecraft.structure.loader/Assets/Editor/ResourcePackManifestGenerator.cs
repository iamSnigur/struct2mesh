using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Editor
{
    public static class ResourcePackManifestGenerator
    {
        private const string TEXTURES_GROUP_NAME   = "Textures";
        private const string MODELS_GROUP_NAME     = "Models";
        private const string BLOCKSTATES_GROUP_NAME = "Blockstates";

        private const string OUTPUT_FOLDER = "Assets/Resources";

        [MenuItem("Tools/ResourcePack/Generate Textures Index")]
        public static void GenerateTexturesIndex() =>
            GenerateIndex(TEXTURES_GROUP_NAME, "textures_manifest_index.json",
                entry => (
                    key:   entry.address[entry.address.IndexOf("block/", StringComparison.Ordinal)..],
                    value: entry.address            
                ));

        [MenuItem("Tools/ResourcePack/Generate Models Index")]
        public static void GenerateModelsIndex() =>
            GenerateIndex(MODELS_GROUP_NAME, "models_manifest_index.json",
                entry => (
                    key:   entry.address[entry.address.IndexOf("block/", StringComparison.Ordinal)..],
                    value: entry.address
                ));

        [MenuItem("Tools/ResourcePack/Generate Blockstates Index")]
        public static void GenerateBlockstatesIndex() =>
            GenerateIndex(BLOCKSTATES_GROUP_NAME, "blockstates_manifest_index.json",
                entry => (
                    key:   Path.GetFileName(entry.address),
                    value: entry.address
                ));

        [MenuItem("Tools/ResourcePack/Generate All Indexes")]
        public static void GenerateAllIndexes()
        {
            GenerateTexturesIndex();
            GenerateModelsIndex();
            GenerateBlockstatesIndex();
        }

        private static void GenerateIndex(
            string groupName,
            string outputFileName,
            Func<AddressableAssetEntry, (string key, string value)> selector)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressables settings not found.");
                return;
            }

            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                Debug.LogError($"Addressables group '{groupName}' not found.");
                return;
            }

            Dictionary<string, string> dict = new();

            foreach (var entry in group.entries)
            {
                if (entry.IsFolder) 
                    continue;

                (string key, string value) = selector(entry);
                
                if (!dict.ContainsKey(key))
                    dict[key] = value;
                else
                    Debug.LogWarning($"[{groupName}] Duplicate key '{key}' — skipping '{value}'");
            }

            WriteJson(outputFileName, dict);
        }

        private static void WriteJson(string fileName, Dictionary<string, string> dict)
        {
            Directory.CreateDirectory(OUTPUT_FOLDER);

            // manual serialization keeps insertion order and avoids JsonUtility limitations
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");

            int i = 0;
            foreach (KeyValuePair<string, string> kvp in dict)
            {
                string comma = ++i < dict.Count ? "," : "";
                sb.AppendLine($"  \"{kvp.Key}\": \"{kvp.Value}\"{comma}");
            }

            sb.AppendLine("}");

            string fullPath = Path.Combine(OUTPUT_FOLDER, fileName);
            File.WriteAllText(fullPath, sb.ToString());
            AssetDatabase.Refresh();

            Debug.Log($"Written {dict.Count} entries → {fullPath}");
        }
    }
}