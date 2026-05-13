using System;
using System.Collections.Generic;
using System.IO;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Editor
{
    public static class ResourcePackManifestGenerator
    {
        private const string TEXTURES_GROUP_NAME    = "Textures";
        private const string MODELS_GROUP_NAME      = "Models";
        private const string BLOCKSTATES_GROUP_NAME = "Blockstates";

        private const string OUTPUT_FOLDER = "Assets/Resources_moved";

        [MenuItem("Tools/ResourcePack/Generate Textures Index")]
        public static void GenerateTexturesIndex() =>
            GenerateIndex(TEXTURES_GROUP_NAME, "textures_manifest_index.json",
                entry => (
                    key:   entry.address[entry.address.IndexOf("block/", StringComparison.Ordinal)..],
                    value: entry.address            
                ));

        /*[MenuItem("Tools/ResourcePack/Generate Models Index")]
        public static void GenerateModelsIndex() =>
            GenerateIndex(MODELS_GROUP_NAME, "models_manifest_index.json",
                entry => (
                    key:   entry.address[entry.address.IndexOf("block/", StringComparison.Ordinal)..],
                    value: entry.address
                ));*/

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
            GenerateModelsIndexWithJson();
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

            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group == null)
            {
                Debug.LogError($"Addressables group '{groupName}' not found.");
                return;
            }

            Dictionary<string, string> dict = new();

            foreach (AddressableAssetEntry entry in group.entries)
            {
                if (entry.IsFolder) 
                    continue;

                (string key, string value) = selector(entry);
                
                if (!dict.TryAdd(key, value))
                    Debug.LogWarning($"[{groupName}] Duplicate key '{key}' — skipping '{value}'");
            }

            string json = JsonConvert.SerializeObject(dict, Formatting.Indented);
            File.WriteAllText(Path.Combine(OUTPUT_FOLDER, outputFileName), json);
            
            AssetDatabase.Refresh();
        }
        
        [MenuItem("Tools/ResourcePack/Generate Models Index With Json")]
        public static void GenerateModelsIndexWithJson() =>
            GenerateModelsIndex(MODELS_GROUP_NAME, "models_manifest_index.json",
                entry => (
                    key:  entry.address[entry.address.IndexOf("block/", StringComparison.Ordinal)..],
                    path: entry.AssetPath
                ));

        private static void GenerateModelsIndex(
            string groupName,
            string outputFileName,
            Func<AddressableAssetEntry, (string key, string path)> selector)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressables settings not found.");
                return;
            }

            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group == null)
            {
                Debug.LogError($"Addressables group '{groupName}' not found.");
                return;
            }

            Dictionary<string, string> dict = new();

            foreach (AddressableAssetEntry entry in group.entries)
            {
                if (entry.IsFolder)
                    continue;

                (string key, string path) = selector(entry);

                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[{groupName}] File not found at path '{path}' — skipping '{key}'");
                    continue;
                }

                string fileContent = File.ReadAllText(path);

                if (!dict.TryAdd(key, fileContent))
                    Debug.LogWarning($"[{groupName}] Duplicate key '{key}' — skipping '{path}'");
            }

            string json = JsonConvert.SerializeObject(dict, Formatting.Indented);
            File.WriteAllText(Path.Combine(OUTPUT_FOLDER, outputFileName), json);
            
            AssetDatabase.Refresh();
        }
    }
}