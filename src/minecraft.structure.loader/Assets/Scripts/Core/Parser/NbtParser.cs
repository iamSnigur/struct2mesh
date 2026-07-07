using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using fNbt;
using Core.Parser.Model;

namespace Core.Parser
{
    public class NbtParser : IParser
    {
        public async UniTask<ParsedStructure> Parse(byte[] fileContent)
        {
            NbtFile nbtFile = GetNbtFile(fileContent);

            if (!IsFileNbtStructure(nbtFile))
                return null;

            ParsedStructure parsedStructure = await ParseNbtFile(nbtFile);

            return parsedStructure;
        }

        private static NbtFile GetNbtFile(byte[] fileContent)
        {
            NbtFile nbtFile = new();
            nbtFile.LoadFromBuffer(fileContent, 0, fileContent.Length, NbtCompression.AutoDetect);
            
            return nbtFile;
        }

        private async UniTask<ParsedStructure> ParseNbtFile(NbtFile nbtFile)
        {
            NbtList blocksTagList = nbtFile.RootTag.Get<NbtList>("blocks");
            NbtList paletteTagList = nbtFile.RootTag.Get<NbtList>("palette") 
                 ?? nbtFile
                     .RootTag
                     .Get<NbtList>("palettes")
                     .Get<NbtList>(0);

            await UniTask.Yield();
            
            ParsedStructure structure = new()
            {
                Blocks = await ParseNbtBlocks(blocksTagList),
                Palette = ParseNbtPalette(paletteTagList)
            };

            return structure;
        }

        private static bool IsFileNbtStructure(NbtFile file)
        {
            NbtTag sizeTag = file.RootTag.Get("size");
            NbtTag blocksTag = file.RootTag.Get("blocks");
            NbtTag paletteTag = file.RootTag.Get("palette");

            if (paletteTag is null)
            {
                NbtList palettesTag = file.RootTag.Get<NbtList>("palettes");
                paletteTag = palettesTag[0];
            }
        
            return sizeTag is not null 
                   && blocksTag is not null
                   && paletteTag is not null;
        }
        
        private async UniTask<List<BlockEntry>> ParseNbtBlocks(NbtList blocksTagList)
        {
            List<BlockEntry> blocks = new();

            float yieldTime = 0f;
            
            foreach (NbtTag nbtTag in blocksTagList)
            {
                var blockTag = (NbtCompound)nbtTag;

                BlockEntry blockEntry = new();
                Vector3 pos = new()
                {
                    x = blockTag["pos"][2].IntValue,
                    y = blockTag["pos"][1].IntValue,
                    z = blockTag["pos"][0].IntValue
                };
                blockEntry.Pos = pos;
                blockEntry.State = blockTag["state"].IntValue;
            
                blocks.Add(blockEntry);
                
                if (yieldTime > 1f)
                {
                    await UniTask.Yield();
                    yieldTime = 0;
                }

                yieldTime += Time.deltaTime;
            }

            return blocks;
        }

        private List<PaletteEntry> ParseNbtPalette(NbtList paletteTagList)
        {
            List<PaletteEntry> palette = new List<PaletteEntry>();

            foreach (NbtTag nbtTag in paletteTagList)
            {
                var paletteTag = (NbtCompound)nbtTag;

                PaletteEntry block = new();
            
                block.Name = paletteTag["Name"].StringValue.Replace("minecraft:", "");

                if (paletteTag.TryGet("Properties", out NbtTag result))
                {
                    var propertiesTag = (NbtCompound)result;

                    if (propertiesTag is not null)
                    {
                        foreach (NbtTag tag in propertiesTag.Tags)
                        {
                            string property = tag.Name;
                            string value = tag.StringValue;

                            block.Properties[property] = value;
                        } 
                    }
                }
            
                palette.Add(block);
            }
        
            return palette;
        }
    }
}