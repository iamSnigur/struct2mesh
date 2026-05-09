/*using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CraftSharp;
using CraftSharp.Resource;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Editor
{
    public static class AtlasAssetsGeneratorTool
    {
        [MenuItem("Tools/Generate Atlas and Indexing")]
        public static void GenerateAtlasAndIndexing()
        {
            
        }
        
        private static void LoadTextures(ResourcePackManager manager, string packPath, string subFolder)
        {
            string path = $"{packPath}/{subFolder}";

            foreach (Texture2D texture in Resources.LoadAll<Texture2D>(path))
            {
                ResourceLocation identifier = new(subFolder + '/' + texture.name);
                manager.TextureFileTable[identifier] = path + '/' + texture.name;
            }
        }

        private static void LoadModels(ResourcePackManager manager, string packPath, string subFolder)
        {
            string path = $"{packPath}/{subFolder}";

            foreach (TextAsset json in Resources.LoadAll<TextAsset>(path))
            {
                ResourceLocation identifier = new(subFolder + '/' + json.name);
                manager.BlockModelFileTable[identifier] = path + '/' + json.name;
            }
        }

        private static void LoadBlockstates(ResourcePackManager manager, string packPath, string subFolder)
        {
            string path = $"{packPath}/{subFolder}";

            foreach (TextAsset json in Resources.LoadAll<TextAsset>(path))
            {
                ResourceLocation identifier = new(json.name);
                manager.BlockStateFileTable[identifier] = path + json.name;
            }
        }

        private static async UniTask GatherResources(ResourcePackManager manager)
        {
            string texturesPath = "vanilla/minecraft/textures";
            LoadTextures(manager, texturesPath, "block");

            string modelsPath = "vanilla/minecraft/models";
            LoadModels(manager, modelsPath, "block");

            string blockstatesPath = "vanilla/minecraft/blockstates";
            LoadBlockstates(manager, blockstatesPath, "");

            //await Resources.UnloadUnusedAssets().ToUniTask();
        }
        
        private static async UniTask GenerateAtlas(DataLoadFlag atlasGenFlag)
        {
            var textureIdSet = new HashSet<ResourceLocation>();

            // Collect referenced textures
            List<string> modelFilePaths = BlockModelFileTable.Values.ToList();

            await GatherTexturesCoroutine(textureIdSet, modelFilePaths);
            
            // Append liquid textures, which are not referenced in model files, but will be used by fluid mesh
            foreach (ResourceLocation texId in FluidGeometry.LiquidTextures)
            {
                if (TextureFileTable.ContainsKey(texId))
                {
                    textureIdSet.Add(texId);
                }
            }

            // Array for textures in collection, plus one blank texture, one empty texture and one missing texture
            int totalCount = 3 + textureIdSet.Count;
            var textureInfos = new (Texture2D, TextureAnimationInfo?)[totalCount];
            var ids = new ResourceLocation[totalCount];

            int count = 0;

            // Blank texture
            ids[count] = BLANK_TEXTURE;
            textureInfos[count] = (GetBlankTexture(), null);
            count++;

            // Empty texture
            ids[count] = EMPTY_TEXTURE;
            textureInfos[count] = (GetEmptyTexture(), null);
            count++;

            // Missing texture
            ids[count] = ResourceLocation.INVALID;
            textureInfos[count] = (GetMissingTexture(), null);
            count++;

            foreach (var textureId in textureIdSet)
            {
                string? texFilePath = TextureFileTable[textureId];
                ids[count] = textureId;
                textureInfos[count++] = LoadSingleTexture_Mod(texFilePath);

                if (count % 5 == 0) await UniTask.NextFrame();
            }
            
            await GenerateAtlas_Internal(atlasGenFlag, textureInfos, ids);
        }
        
        private static IEnumerator GatherTexturesCoroutine(HashSet<ResourceLocation> textureIdSet, List<string> modelFilePaths)
        {
            foreach (string? modelFilePath in modelFilePaths)
            {
                TextAsset jsonModel = Resources.Load<TextAsset>(modelFilePath);
                
                Assert.IsNotNull(jsonModel, "Cannot find json model file: " + modelFilePath);
                Json.JSONData? model = Json.ParseJson(jsonModel.text);

                if (model.Properties.TryGetValue("textures", out var textureObj))
                {
                    Dictionary<string, Json.JSONData>? texData = textureObj.Properties;
                    
                    foreach (var texItem in texData)
                    {
                        if (texItem.Value.StringValue.StartsWith('#')) 
                            continue;
                        
                        ResourceLocation texId = ResourceLocation.FromString(texItem.Value.StringValue);

                        if (TextureFileTable.ContainsKey(texId))
                            textureIdSet.Add(texId);
                    }
                }

                yield return null;
            }
        }
        
        private static readonly Dictionary<ResourceLocation, TextureInfo> texAtlasTable = new();
        
        private static IEnumerator GenerateAtlas_Internal(DataLoadFlag atlasGenFlag, (Texture2D, TextureAnimationInfo?)[] textureInfos, ResourceLocation[] ids)
        {
            texAtlasTable.Clear(); // Clear previously loaded table...
            
            int curTexIndex = 0, curAtlasIndex = 0;
            var totalCount = textureInfos.Length;
            List<Texture2D> atlases = new();

            const int totalVolume = ATLAS_SIZE * ATLAS_SIZE;
            const int maxContentVolume = (int)(totalVolume * 0.97F);

            do
            {
                // First count all the textures to be stitched onto this atlas
                int lastTexIndex = curTexIndex - 1, curVolume = 0; // lastTexIndex is inclusive

                while (lastTexIndex < totalCount - 1)
                {
                    var (nextTex, _) = textureInfos[lastTexIndex + 1];
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
                var textureInfosConsumed = textureInfos[curTexIndex..(lastTexIndex + 1)];

                // First assign a placeholder
                var atlas = new Texture2D(ATLAS_SIZE, ATLAS_SIZE)
                {
                    filterMode = FilterMode.Point
                };

                var rects = atlas.PackTextures(textureInfosConsumed.Select(x => x.Item1).ToArray(), 0, ATLAS_SIZE, false);

                if (atlas.width != ATLAS_SIZE || atlas.height != ATLAS_SIZE)
                {
                    // Size not right, replace it (usually the last atlas in array which doesn't
                    // have enough textures to take up all the place and thus is smaller in size)
                    var newAtlas = new Texture2D(ATLAS_SIZE, ATLAS_SIZE);

                    Graphics.CopyTexture(atlas, 0, 0, 0, 0, atlas.width, atlas.height, newAtlas, 0, 0, 0, 0);

                    float scaleX = atlas.width  / (float) ATLAS_SIZE;
                    float scaleY = atlas.height / (float) ATLAS_SIZE;

                    // Rescale the texture boundaries
                    for (int i = 0;i < rects.Length;i++)
                    {
                        rects[i] = new Rect(
                            rects[i].x     * scaleX,    rects[i].y      * scaleY,
                            rects[i].width * scaleX,    rects[i].height * scaleY
                        );
                    }

                    atlas = newAtlas;
                }

                atlases.Add(atlas);

                yield return null;

                for (int i = 0;i < consumedTexCount;i++)
                {
                    //Debug.Log($"{curTexIndex + i} {ids[curTexIndex + i]} => ({curAtlasIndex}) {rects[i].xMin} {rects[i].xMax} {rects[i].yMin} {rects[i].yMax}");
                    var curAnimInfo = textureInfos[curTexIndex + i].Item2;
                    
                    if (curAnimInfo is null)
                        texAtlasTable.Add(ids[curTexIndex + i], new TextureInfo(rects[i], curAtlasIndex));
                    else
                        texAtlasTable.Add(ids[curTexIndex + i], new TextureInfo(rects[i], curAtlasIndex, curAnimInfo.frameCount,
                                rects[i].width / curAnimInfo.framePerRow, curAnimInfo.framePerRow, curAnimInfo.interpolate, curAnimInfo.frameInterval));
                }

                curTexIndex += consumedTexCount;
                curAtlasIndex++;

                yield return null;
            }
            while (curTexIndex < totalCount);

            // save table
            
            
            // mipCount 1 means no mipmapping, mipCount 0 means use maximum level of mipmaps
            // A mipCount of ((int) math.log2(MIN_MIPMAP_SIZE)) + 1 makes sure that all
            // MIN_MIPMAP_SIZE * MIN_MIPMAP_SIZE mipmap chunks are sampled from a single tile
            var mipCount1 = ((int) math.log2(MIN_MIPMAP_SIZE)) + 1;
            var mipCount0 = Mathf.Max(1, mipCount1 - 2);
            var atlasArray0 = new Texture2DArray(ATLAS_SIZE, ATLAS_SIZE, curAtlasIndex, TextureFormat.RGBA32, mipCount0, false);
            var atlasArray1 = new Texture2DArray(ATLAS_SIZE, ATLAS_SIZE, curAtlasIndex, TextureFormat.RGBA32, mipCount1, false);

            atlasArray0.filterMode = FilterMode.Point;
            atlasArray1.filterMode = FilterMode.Point;

            for (int index = 0;index < atlases.Count;index++)
            {
                // Set texture pixels
                atlasArray0.SetPixels32(atlases[index].GetPixels32(), index, 0);
                atlasArray1.SetPixels32(atlases[index].GetPixels32(), index, 0);

                // Generate mipmaps of different levels
                var lastColors = atlases[index].GetPixels32();
                var pixColors = new Color32[4];

                for (int mipLevel = 1, size = ATLAS_SIZE; mipLevel < mipCount1; mipLevel++, size >>= 1)
                {
                    int mipSize = size >> 1;
                    var mipColors = new Color32[mipSize * mipSize];

                    for (int y = 0; y < mipSize; y++)
                        for (int x = 0; x < mipSize; x++)
                        {
                            pixColors[0] = lastColors[ y * 2      * size + x * 2    ];
                            pixColors[1] = lastColors[ y * 2      * size + x * 2 + 1];
                            pixColors[2] = lastColors[(y * 2 + 1) * size + x * 2    ];
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
                                byte blendR = (byte) Mathf.RoundToInt(Mathf.LinearToGammaSpace(r / pixCount) * 255F);
                                byte blendG = (byte) Mathf.RoundToInt(Mathf.LinearToGammaSpace(g / pixCount) * 255F);
                                byte blendB = (byte) Mathf.RoundToInt(Mathf.LinearToGammaSpace(b / pixCount) * 255F);

                                mipColors[y * mipSize + x] = new Color32(blendR, blendG, blendB, (byte) (a / pixCount));
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

            AtlasTableHelper.SaveAtlasTable(texAtlasTable);
            Save(atlasArray1, "Assets/Resources/vanilla/minecraft/textures/block_atlas.asset");
            Save(atlasArray0, "Assets/Resources/vanilla/minecraft/textures/block_atlas_mip.asset");
            
            atlasArrays[0] = atlasArray0;
            atlasArrays[1] = atlasArray1;
            
            atlasGenFlag.Finished = true;
        }

        private static void Save(Texture2DArray array, string path)
        {
#if UNITY_EDITOR
            AssetDatabase.CreateAsset(array, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
    }
}*/