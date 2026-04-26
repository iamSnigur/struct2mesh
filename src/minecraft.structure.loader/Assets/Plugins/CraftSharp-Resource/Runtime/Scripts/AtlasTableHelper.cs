using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CraftSharp.Resource
{
    public class AtlasTableHelper
    {
        [Serializable]
        private record TextureInfoEntry
        {
            public string PathKey;
            public TextureInfo InfoValue;
        }

        [Serializable]
        private record EntriesWrapper
        {
            public TextureInfoEntry[] Entries;
        }
    
        public static void SaveAtlasTable(Dictionary<ResourceLocation, TextureInfo> atlasTable)
        {
            TextureInfoEntry[] entries = atlasTable.Select(pair => new TextureInfoEntry
            {
                PathKey = pair.Key.Path,
                InfoValue = pair.Value
            }).ToArray();

            EntriesWrapper wrapper = new()
            {
                Entries = entries
            };

            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText("Assets/Resources/vanilla/minecraft/textures/texture_atlas_table.json", json);
            //AssetDatabase.Refresh();
        }

        public static void LoadAtlasTable(Dictionary<ResourceLocation, TextureInfo> atlasTable)
        {
            TextAsset jsonAsset = Resources.Load<TextAsset>("vanilla/minecraft/textures/texture_atlas_table");
            EntriesWrapper wrapper = JsonUtility.FromJson<EntriesWrapper>(jsonAsset.text);

            foreach (TextureInfoEntry entry in wrapper.Entries) 
                atlasTable.Add(new ResourceLocation(entry.PathKey), entry.InfoValue);
            
        }
    }
}