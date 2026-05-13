using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.AssetManagement.External;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace CraftSharp.Resource
{
    public static class AtlasTableHelper
    {
        [Serializable]
        private record TextureInfoEntry
        {
            public string PathKey;
            public TextureInfo InfoValue;
        }
    
        public static void SaveAtlasTable(Dictionary<ResourceLocation, TextureInfo> atlasTable)
        {
            TextureInfoEntry[] entries = atlasTable.Select(pair => new TextureInfoEntry
            {
                PathKey = pair.Key.Path,
                InfoValue = pair.Value
            }).ToArray();

            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            string json = JsonConvert.SerializeObject(entries, Formatting.Indented, settings);
            
            File.WriteAllText("Assets/Resources_moved/vanilla/minecraft/textures/texture_atlas_manifest.json", json);
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        public static async UniTask LoadAtlasTable(Dictionary<ResourceLocation, TextureInfo> atlasTable)
        {
            TextAsset jsonAsset = await AssetProvider.LoadByAddressAsync<TextAsset>("texture_atlas_manifest");
            TextureInfoEntry[] manifestEntries = JsonConvert.DeserializeObject<TextureInfoEntry[]>(jsonAsset.text);

            foreach (TextureInfoEntry entry in manifestEntries) 
                atlasTable.Add(new ResourceLocation(entry.PathKey), entry.InfoValue);
        }
    }
}