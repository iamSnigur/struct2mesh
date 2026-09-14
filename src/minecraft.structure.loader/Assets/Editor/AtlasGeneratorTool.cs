using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Core.AssetManagement.External;
using Core.Async;
using CraftSharp;
using CraftSharp.Resource;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Color = UnityEngine.Color;
using Graphics = UnityEngine.Graphics;

namespace Editor
{
    #nullable enable
    public static class AtlasGeneratorTool
    {
        private record TextureAnimationInfo
        {
            public readonly int framePerRow;
            public readonly int frameCount;
            public readonly float frameInterval;
            public readonly bool interpolate;

            public TextureAnimationInfo(int f, int fRow, float i, bool itpl)
            {
                frameCount = f;
                framePerRow = fRow;
                frameInterval = i;
                interpolate = itpl;
            }
        }
        
        private struct AtlasConfig
        {
            public string AtlasAssetName;
            public string AtlasIndexName;
            public int Size;
            public bool ExtrudeEdges;
            public int ExtrudeBorder;
        }
        
        private static readonly ResourceLocation BLANK_TEXTURE = new("blank");
        private static readonly ResourceLocation EMPTY_TEXTURE = new("empty");
        
        private const int MIN_MIPMAP_SIZE = 16;
        
        private static readonly Dictionary<ResourceLocation, string> s_blockTexturesTable = new(); 
        private static readonly Dictionary<ResourceLocation, string> s_blockEntityTexturesTable = new(); 
        
        private static readonly Dictionary<ResourceLocation, TextureInfo> s_textureAtlasTable = new();
        
        [MenuItem("Tools/Atlas/Generate Block Atlas")]
        public static async UniTaskVoid GenerateAtlasAndIndexing()
        {
            await LoadLocationsAsync(s_blockTexturesTable, Constants.BLOCK_TEXTURES_INDEX);
            Debug.Log("Block locations loaded");

            AtlasConfig blockAtlasConfig = new()
            {
                AtlasAssetName = Constants.BLOCK_ATLAS,
                AtlasIndexName = Constants.BLOCK_ATLAS_INDEX,
                Size = 2048,
                ExtrudeEdges = true,
                ExtrudeBorder = 16
            };
            
            await GenerateAtlasAsync(s_blockTexturesTable, blockAtlasConfig);
            
            Debug.Log("Block Atlas assets generated");
        }
        
        [MenuItem("Tools/Atlas/Generate Block Entity Atlas")]
        public static async UniTaskVoid GenerateBlockEntityAtlas()
        {
            await LoadLocationsAsync(s_blockEntityTexturesTable, Constants.BLOCK_ENTITY_TEXTURES_INDEX);
            Debug.Log("Block Entity locations loaded");
            
            AtlasConfig blockEntityAtlasConfig = new()
            {
                AtlasAssetName = Constants.BLOCK_ENTITY_ATLAS,
                AtlasIndexName = Constants.BLOCK_ENTITY_ATLAS_INDEX,
                Size = 1024,
                ExtrudeEdges = false
            };
            
            await GenerateAtlasAsync(s_blockEntityTexturesTable, blockEntityAtlasConfig);
            
            Debug.Log("Block Entity Atlas assets generated");
        }
        
        private static async UniTask LoadLocationsAsync(
            Dictionary<ResourceLocation, string> table,
            string indexAddress)
        {
            TextAsset indexAsset = await AssetProvider.LoadByAddressAsync<TextAsset>(indexAddress);
            var indexes = JsonConvert.DeserializeObject<Dictionary<string, string>>(indexAsset.text)!;

            table.Clear();

            LongTaskYielder yielder = new();
            
            foreach ((string id, string path) in indexes)
            {
                if (id.Contains(".mcmeta"))
                    continue;
                
                ResourceLocation identifier = new(id);
                table[identifier] = path;
                
                await yielder.TryYieldAsync();
            }
        }
        
        private static async UniTask GenerateAtlasAsync(
            Dictionary<ResourceLocation, string> textureTable,
            AtlasConfig atlasConfig)
        {
            // Array for textures in collection, plus one blank texture, one empty texture and one missing texture
            int totalCount = 3 + textureTable.Count;
            var textureInfos = new (Texture2D, TextureAnimationInfo?)[totalCount];
            ResourceLocation[] textureLocations = new ResourceLocation[totalCount];

            int count = 0;

            // Blank texture
            textureLocations[count] = BLANK_TEXTURE;
            textureInfos[count] = (GetBlankTexture(), null);
            count++;

            // Empty texture
            textureLocations[count] = EMPTY_TEXTURE;
            textureInfos[count] = (GetEmptyTexture(), null);
            count++;

            // Missing texture
            textureLocations[count] = ResourceLocation.INVALID;
            textureInfos[count] = (GetMissingTexture(), null);
            count++;

            var textureTasks = new UniTask<(Texture2D, TextureAnimationInfo?)>[textureTable.Count];

            int i = 0;
            foreach ((ResourceLocation textureId, string texturePath) in textureTable)
            {
                textureLocations[count + i] = textureId;
                textureTasks[i] = LoadSingleTextureAsync(texturePath);
                ++i;
            }

            (Texture2D, TextureAnimationInfo?)[] textureData = await UniTask.WhenAll(textureTasks);

            foreach (var data in textureData)
                textureInfos[count++] = data;

            await GenerateAtlasAsync_Internal(
                textureInfos,
                textureLocations,
                atlasConfig);
        }
        
        private static async UniTask<(Texture2D, TextureAnimationInfo?)> LoadSingleTextureAsync(string texturePath)
        {
            Texture2D tex = await AssetProvider.LoadByAddressAsync<Texture2D>(texturePath);
            Assert.IsNotNull(tex, $"Failed to load texture: {texturePath}");

            string mcmeta = $"{texturePath}.mcmeta";

            if (!await AssetProvider.KeyExistsAsync<TextAsset>(mcmeta))
                return (tex, null);

            TextAsset metaAsset = await AssetProvider.LoadByAddressAsync<TextAsset>(mcmeta);

            int spriteCount = tex.height / tex.width;

            Json.JSONData? metaJson = Json.ParseJson(metaAsset.text);

            if (!metaJson.Properties.TryGetValue("animation", out var animJson))
                return (tex, null);

            int[] frames;

            if (animJson.Properties.TryGetValue("frames", out var frameArray)) // Place the frames in specified order
            {
                frames = frameArray.DataArray.Select(x => int.Parse(x.StringValue)).ToArray();
            }
            else // Place the frames in ordinal order, from top to bottom
            {
                frames = Enumerable.Range(0, spriteCount).ToArray();
            }

            int frameCount = frames.Length;

            if (frameCount <= 1)
                return (tex, null);

            float frameInterval;

            if (animJson.Properties.TryGetValue("frametime", out var frameTime))
                frameInterval = float.Parse(frameTime.StringValue, CultureInfo.InvariantCulture.NumberFormat) * 0.05F;
            else // Use default frame interval
                frameInterval = 0.05F;

            var interpolate = animJson.Properties.TryGetValue("interpolate", out var interpolation)
                              && interpolation.StringValue.ToLower().Equals("true");

            int frameSize = tex.width;

            int framePerRow = Mathf.CeilToInt(math.sqrt(frameCount));
            int framePerCol = Mathf.CeilToInt((float)frameCount / framePerRow);

            // Re-arrange the texture
            Texture2D rearranged = new(framePerRow * frameSize, framePerCol * frameSize);

            for (int fi = 0; fi < frameCount; fi++)
            {
                int framePos = frames[fi];

                // Copy pixel data
                Graphics.CopyTexture(tex, 0, 0, 0, (spriteCount - 1 - framePos) * frameSize, frameSize, frameSize,
                    rearranged, 0, 0, (fi % framePerRow) * frameSize, (framePerCol - 1 - fi / framePerRow) * frameSize);
            }

            return (rearranged, new TextureAnimationInfo(frameCount, framePerRow, frameInterval, interpolate));
        }

        private static Texture2D ExtrudeTexture(Texture2D src, int border)
        {
            int w = src.width;
            int h = src.height;
            var dst = new Texture2D(w + border * 2, h + border * 2, TextureFormat.RGBA32, false)
            {
                filterMode = src.filterMode
            };

            Color32[] srcPx = src.GetPixels32();
            var dstPx = new Color32[dst.width * dst.height];

            for (int y = 0; y < dst.height; y++)
                for (int x = 0; x < dst.width; x++)
                {
                    int sx = Mathf.Clamp(x - border, 0, w - 1);
                    int sy = Mathf.Clamp(y - border, 0, h - 1);
                    dstPx[y * dst.width + x] = srcPx[sy * w + sx];
                }

            dst.SetPixels32(dstPx);
            dst.Apply(false);
            return dst;
        }
        
        private static IEnumerator GenerateAtlasAsync_Internal(
            (Texture2D, TextureAnimationInfo?)[] textureInfos,
            ResourceLocation[] ids,
            AtlasConfig atlasConfig)
        {
            s_textureAtlasTable.Clear(); // Clear previously loaded table...

            if (atlasConfig.ExtrudeEdges)
            {
                for (int i = 0; i < textureInfos.Length; i++)
                {
                    (Texture2D tex, TextureAnimationInfo? anim) = textureInfos[i];
                    textureInfos[i] = (ExtrudeTexture(tex, atlasConfig.ExtrudeBorder), anim);
                }
            }
            
            int atlasSize = atlasConfig.Size;
            int curTexIndex = 0, curAtlasIndex = 0;
            var totalCount = textureInfos.Length;
            List<Texture2D> atlases = new();

            int totalVolume = atlasSize * atlasSize;
            int maxContentVolume = (int)(totalVolume * 0.97F);

            do
            {
                // First count all the textures to be stitched onto this atlas
                int lastTexIndex = curTexIndex - 1, curVolume = 0; // lastTexIndex is inclusive

                while (lastTexIndex < totalCount - 1)
                {
                    (Texture2D nextTex, _) = textureInfos[lastTexIndex + 1];
                    Assert.IsNotNull(nextTex, $"Texture {ids[lastTexIndex + 1]} is null");
                    
                    curVolume += nextTex.width * nextTex.height;

                    if (curVolume < maxContentVolume)
                        lastTexIndex++;
                    else
                        break;
                }

                int consumedTexCount = lastTexIndex + 1 - curTexIndex;

                if (consumedTexCount == 0)
                {
                    // In this occasion the texture is too large and can only be scaled a bit and placed on a separate atlas
                    lastTexIndex = curTexIndex;
                    consumedTexCount = 1;
                }

                // Then we go stitch 'em        (inclusive)..(exclusive)
                (Texture2D, TextureAnimationInfo?)[] textureInfosConsumed =
                    textureInfos[curTexIndex..(lastTexIndex + 1)];

                // First assign a placeholder
                var atlas = new Texture2D(atlasSize, atlasSize)
                {
                    filterMode = FilterMode.Point
                };

                Rect[]? rects = atlas.PackTextures(
                    textureInfosConsumed.Select(x => x.Item1).ToArray(),
                    0,
                    atlasSize,
                    false);

                if (atlas.width != atlasSize || atlas.height != atlasSize)
                {
                    // Size not right, replace it (usually the last atlas in array which doesn't
                    // have enough textures to take up all the place and thus is smaller in size)
                    var newAtlas = new Texture2D(atlasSize, atlasSize);

                    Graphics.CopyTexture(atlas, 0, 0, 0, 0, atlas.width, atlas.height, newAtlas, 0, 0, 0, 0);

                    float scaleX = atlas.width / (float)atlasSize;
                    float scaleY = atlas.height / (float)atlasSize;

                    // Rescale the texture boundaries
                    for (int i = 0; i < rects.Length; i++)
                    {
                        rects[i] = new Rect(
                            rects[i].x * scaleX, rects[i].y * scaleY,
                            rects[i].width * scaleX, rects[i].height * scaleY
                        );
                    }

                    atlas = newAtlas;
                }

                atlases.Add(atlas);

                yield return null;
                
                if (atlasConfig.ExtrudeEdges)
                {
                    float inset = atlasConfig.ExtrudeBorder / (float)atlasSize;

                    for (int i = 0; i < rects.Length; i++)
                        rects[i] = new Rect(
                            rects[i].x + inset,
                            rects[i].y + inset,
                            rects[i].width - inset * 2,
                            rects[i].height - inset * 2);
                }

                for (int i = 0; i < consumedTexCount; i++)
                {
                    //Debug.Log($"{curTexIndex + i} {ids[curTexIndex + i]} => ({curAtlasIndex}) {rects[i].xMin} {rects[i].xMax} {rects[i].yMin} {rects[i].yMax}");
                    var curAnimInfo = textureInfos[curTexIndex + i].Item2;

                    if (curAnimInfo is null)
                        s_textureAtlasTable.Add(ids[curTexIndex + i], new TextureInfo(rects[i], curAtlasIndex));
                    else
                        s_textureAtlasTable.Add(ids[curTexIndex + i], new TextureInfo(rects[i], curAtlasIndex,
                            curAnimInfo.frameCount,
                            rects[i].width / curAnimInfo.framePerRow, curAnimInfo.framePerRow, curAnimInfo.interpolate,
                            curAnimInfo.frameInterval));
                }

                curTexIndex += consumedTexCount;
                curAtlasIndex++;

                yield return null;
            } while (curTexIndex < totalCount);

            // mipCount 1 means no mipmapping, mipCount 0 means use maximum level of mipmaps
            // A mipCount of ((int) math.log2(MIN_MIPMAP_SIZE)) + 1 makes sure that all
            // MIN_MIPMAP_SIZE * MIN_MIPMAP_SIZE mipmap chunks are sampled from a single tile
            var mipCount1 = ((int)math.log2(MIN_MIPMAP_SIZE)) + 1;
            var mipCount0 = Mathf.Max(1, mipCount1 - 2);
            var atlasArray0 = new Texture2DArray(
                atlasSize, atlasSize,
                curAtlasIndex,
                TextureFormat.RGBA32,
                mipCount0,
                false);
            var atlasArray1 = new Texture2DArray(atlasSize, atlasSize,
                curAtlasIndex,
                TextureFormat.RGBA32,
                mipCount1,
                false);

            atlasArray0.filterMode = FilterMode.Point;
            atlasArray1.filterMode = FilterMode.Point;

            for (int index = 0; index < atlases.Count; index++)
            {
                // Set texture pixels
                atlasArray0.SetPixels32(atlases[index].GetPixels32(), index, 0);
                atlasArray1.SetPixels32(atlases[index].GetPixels32(), index, 0);

                // Generate mipmaps of different levels
                Color32[] lastColors = atlases[index].GetPixels32();
                var pixColors = new Color32[4];

                for (int mipLevel = 1, size = atlasSize; mipLevel < mipCount1; mipLevel++, size >>= 1)
                {
                    int mipSize = size >> 1;
                    var mipColors = new Color32[mipSize * mipSize];

                    for (int y = 0; y < mipSize; y++)
                        for (int x = 0; x < mipSize; x++)
                        {
                            pixColors[0] = lastColors[y * 2 * size + x * 2];
                            pixColors[1] = lastColors[y * 2 * size + x * 2 + 1];
                            pixColors[2] = lastColors[(y * 2 + 1) * size + x * 2];
                            pixColors[3] = lastColors[(y * 2 + 1) * size + x * 2 + 1];

                            float r = 0, g = 0, b = 0;
                            int a = 0;
                            int pixCount = 0;

                            for (int p = 0; p < 4; p++)
                            {
                                if (pixColors[p].a > 0) // Discard fully transparent pixels
                                {
                                    r += Mathf.GammaToLinearSpace(pixColors[p].r / 255F);
                                    g += Mathf.GammaToLinearSpace(pixColors[p].g / 255F);
                                    b += Mathf.GammaToLinearSpace(pixColors[p].b / 255F);
                                    a += pixColors[p].a; // Alpha is linear

                                    pixCount++;
                                }
                            }

                            if (pixCount > 2 || (pixCount == 2 && (x + y) % 2 == 0))
                            {
                                byte blendR = (byte)Mathf.RoundToInt(Mathf.LinearToGammaSpace(r / pixCount) * 255F);
                                byte blendG = (byte)Mathf.RoundToInt(Mathf.LinearToGammaSpace(g / pixCount) * 255F);
                                byte blendB = (byte)Mathf.RoundToInt(Mathf.LinearToGammaSpace(b / pixCount) * 255F);

                                mipColors[y * mipSize + x] = new Color32(blendR, blendG, blendB, (byte)(a / pixCount));
                            }
                            else // The composited pixel is transparent
                            {
                                mipColors[y * mipSize + x] = Color.clear;
                            }
                        }

                    if (mipLevel < mipCount0)
                    {
                        atlasArray0.SetPixels32(mipColors, index, mipLevel);
                    }

                    atlasArray1.SetPixels32(mipColors, index, mipLevel);

                    lastColors = mipColors; // Continue to generate next level of mipmap
                }

                yield return null;
            }

            atlasArray0.Apply(false, false);
            atlasArray1.Apply(false, false);

            AtlasIndexHelper.Save(s_textureAtlasTable, atlasConfig.AtlasIndexName);
            Save(atlasArray1, atlasConfig.AtlasAssetName);
        }

        private static void Save(Texture2DArray array, string name)
        {
            int sliceIndex = 0;
            Texture2D tex = new(array.width, array.height, array.format, array.mipmapCount, false)
            {
                filterMode = FilterMode.Point
            };
            Graphics.CopyTexture(array, sliceIndex, tex, 0);
            tex.Apply();

            string path = $"{Constants.ATLAS_PATH}/{name}.asset";
            AssetDatabase.CreateAsset(tex, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        private static Texture2D GetBlankTexture()
        {
            Texture2D tex = new(16, 16);
            Color32 white = Color.white;

            var colors = Enumerable.Repeat(white, 16 * 16).ToArray();
            tex.SetPixels32(colors);
            // No need to update mipmap because it'll be
            // stitched into the atlas later
            tex.Apply(false);

            return tex;
        }

        private static Texture2D GetEmptyTexture()
        {
            return GetSolidTexture(new Color32(0, 0, 0, 0));
        }

        private static Texture2D GetSolidTexture(Color32 color)
        {
            Texture2D tex = new(16, 16);

            var colors = Enumerable.Repeat(color, 16 * 16).ToArray();
            tex.SetPixels32(colors);
            // No need to update mipmap because it'll be
            // stitched into the atlas later
            tex.Apply(false);

            return tex;
        }

        public static Texture2D GetMissingTexture()
        {
            Texture2D tex = new(16, 16);
            Color32 black = Color.black;
            Color32 magenta = Color.magenta;

            var colors = new Color32[16 * 16];

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                    colors[(i << 4) + j] = black;

                for (int j = 8; j < 16; j++)
                    colors[(i << 4) + j] = magenta;
            }

            for (int i = 8; i < 16; i++)
            {
                for (int j = 0; j < 8; j++)
                    colors[(i << 4) + j] = magenta;

                for (int j = 8; j < 16; j++)
                    colors[(i << 4) + j] = black;
            }

            tex.SetPixels32(colors);
            // No need to update mipmap because it'll be
            // stitched into the atlas later
            tex.Apply(false);

            return tex;
        }
    }
}