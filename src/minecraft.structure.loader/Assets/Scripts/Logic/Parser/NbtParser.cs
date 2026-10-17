using System.Collections.Generic;
using Core.Async;
using Cysharp.Threading.Tasks;
using fNbt;
using Logic.Parser.Model;
using UnityEngine;

namespace Logic.Parser
{
    public class NbtParser : IParser
    {
        // workaround to prevent OOM on web, but it's still happening
        private const int BLOCK_COUNT_LIMIT = 200_000;
        
        public async UniTask<ParserResult> Parse(byte[] fileContent)
        {
            NbtFile nbtFile = GetNbtFile(fileContent);

            await UniTask.Yield();

            if (!IsFileNbtStructure(nbtFile))
                return ParserResult.Failed("Nbt file is not a structure, or doesn't have a valid format");

#if CHECK_BLOCK_LIMIT && !UNITY_EDITOR
             if (IsBlockCountExceededLimits(nbtFile))
                return ParserResult.Failed($"Structure exceeded the '{BLOCK_COUNT_LIMIT:N0}' block limit");
#endif

            ParsedStructure structure = await ParseNbtFile(nbtFile);

            return ParserResult.Succeeded(structure);
        }

        private static NbtFile GetNbtFile(byte[] fileContent)
        {
            NbtFile nbtFile = new();
            nbtFile.LoadFromBuffer(fileContent, 0, fileContent.Length, NbtCompression.AutoDetect);
            
            return nbtFile;
        }

        private async UniTask<ParsedStructure> ParseNbtFile(NbtFile nbtFile)
        {
            NbtList sizeTagList = nbtFile.RootTag.Get<NbtList>("size");
            NbtList blocksTagList = nbtFile.RootTag.Get<NbtList>("blocks");
            NbtList paletteTagList = nbtFile.RootTag.Get<NbtList>("palette") 
                 ?? nbtFile
                     .RootTag
                     .Get<NbtList>("palettes")!
                     .Get<NbtList>(0);

            ParsedStructure structure = new()
            {
                Size = ParseNbtSize(sizeTagList),
                Blocks = await ParseNbtBlocks(blocksTagList),
                Palette = ParseNbtPalette(paletteTagList)
            };

            return structure;
        }

        private static Vector3 ParseNbtSize(NbtList sizeTagList)
        {
            float x = sizeTagList[2].IntValue;
            float y = sizeTagList[1].IntValue;
            float z = sizeTagList[0].IntValue;

            return new Vector3(x, y, z);
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

        private static bool IsBlockCountExceededLimits(NbtFile file)
        {
            NbtList blocksTagList = file.RootTag.Get<NbtList>("blocks")!;
            return blocksTagList.Count > BLOCK_COUNT_LIMIT;
        }
        
        private async UniTask<List<BlockEntry>> ParseNbtBlocks(NbtList blocksTagList)
        {
            List<BlockEntry> blocks = new();

            LongTaskYielder yielder = new();
            
            foreach (NbtTag nbtTag in blocksTagList)
            {
                BlockEntry blockEntry = new();
                NbtCompound blockTag = (NbtCompound)nbtTag;

                blockEntry.Pos = GetPosition(blockTag);
                blockEntry.State = blockTag["state"].IntValue;
            
                blocks.Add(blockEntry);

                await yielder.TryYieldAsync();
            }

            return blocks;
        }

        private static Vector3 GetPosition(NbtCompound blockTag)
        {
            if (blockTag["pos"] is NbtIntArray array)
                return new Vector3
                {
                    x = array[2],
                    y = array[1],
                    z = array[0],
                };
            
            if (blockTag["pos"] is NbtList list)
                return new Vector3
                {
                    x = list[2].IntValue,
                    y = list[1].IntValue,
                    z = list[0].IntValue
                };

            return Vector3.zero;
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