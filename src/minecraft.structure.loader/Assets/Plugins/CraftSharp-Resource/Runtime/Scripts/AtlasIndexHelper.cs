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
    public static class AtlasIndexHelper
    {
        [Serializable]
        private struct TextureInfoEntry
        {
            public string PathKey;
            public TextureInfoDto InfoValue;
        }
        
        [Serializable]
        private struct TextureInfoDto
        {
            public float xOffset;
            public float yOffset;
            public float width;
            public float height;
            public int index;
            public int frameCount;
            public float frameSize;
            public int framePerRow;
            public bool interpolate;
            public float frameInterval;
        }

        private static TextureInfoDto ToDto(in TextureInfo info)
        {
            return new TextureInfoDto
            {
                xOffset = info.bounds.x,
                yOffset = info.bounds.y,
                width = info.bounds.width,
                height = info.bounds.height,

                index = info.index,
                frameCount = info.frameCount,
                frameSize = info.frameSize,
                framePerRow = info.framePerRow,
                interpolate = info.interpolate,
                frameInterval = info.frameInterval
            };
        }

        private static TextureInfo ToInfo(in TextureInfoDto dto)
        {
            return new TextureInfo
            {
                bounds = new Rect(dto.xOffset, dto.yOffset, dto.width, dto.height),
                frameCount = dto.frameCount,
                frameInterval = dto.frameInterval,
                framePerRow = dto.framePerRow,
                frameSize = dto.frameSize,
                index = dto.index,
                interpolate = dto.interpolate
            };
        }
    
        public static void Save(
            Dictionary<ResourceLocation, TextureInfo> atlasTable,
            string atlasName)
        {
            TextureInfoEntry[] entries = atlasTable.Select(pair => new TextureInfoEntry
            {
                PathKey = pair.Key.Path,
                InfoValue = ToDto(pair.Value)
            }).ToArray();

            string json = JsonConvert.SerializeObject(entries, Formatting.Indented);
            
            File.WriteAllText($"Assets/Resources_moved/vanilla/minecraft/textures/{atlasName}.json", json);
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        public static async UniTask LoadAsync(
            Dictionary<ResourceLocation, TextureInfo> atlasTable,
            string atlasName)
        {
            TextAsset jsonAsset = await AssetProvider.LoadByAddressAsync<TextAsset>(atlasName);
            TextureInfoEntry[] indexEntries = JsonConvert.DeserializeObject<TextureInfoEntry[]>(jsonAsset.text);

            foreach (TextureInfoEntry entry in indexEntries) 
                atlasTable.Add(new ResourceLocation(entry.PathKey), ToInfo(entry.InfoValue));
        }
    }
}