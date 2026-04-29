using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

using CraftSharp.Protocol.Message;

namespace CraftSharp.Rendering
{
    public class SignBlockEntityRender : BaseSignBlockEntityRender
    {
        private static readonly ResourceLocation SIGN_ID = new("sign");
        private static readonly ResourceLocation WALL_SIGN_ID = new("wall_sign");

        public override void UpdateBlockState(BlockState blockState, bool isItemPreview)
        {
            isItem = isItemPreview;
            
            if (blockState != BlockState)
            {
                var variant = blockState.BlockId.Path;
                bool isWall = variant.EndsWith("_wall_sign");

                variant = isWall ? variant[..^"_wall_sign".Length] : variant[..^"_sign".Length];
                
                centerTransform = transform.GetChild(0); // Get 1st child
                // Destroy previous block entity render, but preserve the text object
                foreach (Transform t in transform)
                {
                    if (t != centerTransform)
                        Destroy(t.gameObject);
                }

                var render = BuildBedrockBlockEntityRender(isWall ? WALL_SIGN_ID : SIGN_ID);
                
                render.transform.localScale = Vector3.one * 2F / 3F;
                render.transform.localPosition = BEDROCK_BLOCK_ENTITY_OFFSET;

                if (centerTransform)
                {
                    if (blockState.Properties.TryGetValue("rotation", out var rotationVal))
                    {
                        float rotationDeg = (short.Parse(rotationVal) + 4) % 16 * 22.5F;
                        centerTransform.localEulerAngles = new(0F, rotationDeg, 0F);
                        centerTransform.localPosition = new(0F, 0.33334F, 0F);
                        
                        render.transform.localEulerAngles = new(0F, rotationDeg + 90, 0F);
                    }
                    else if (blockState.Properties.TryGetValue("facing", out var facingVal))
                    {
                        int rotationDeg = facingVal switch
                        {
                            "north" => 270,
                            "east"  => 0,
                            "south" => 90,
                            "west"  => 180,
                            _       => 0
                        };
                        centerTransform.localEulerAngles = new(0F, rotationDeg, 0F);
                        centerTransform.localPosition = facingVal switch
                        {
                            "north" => new Vector3( 0.4375F, 0F, 0F),
                            "east"  => new Vector3(0F, 0F, -0.4375F),
                            "south" => new Vector3(-0.4375F, 0F, 0F),
                            "west"  => new Vector3(0F, 0F,  0.4375F),
                            _       => new Vector3(0F, 0F, 0F)
                        };
                        
                        render.transform.localEulerAngles = new(0F, rotationDeg + 90, 0F);
                    }   
                }
                
                var entityName = isWall ? "wall_sign" : "sign";
                SetBedrockBlockEntityRenderTexture(render, $"{entityName}/{variant}");
            }
            
            base.UpdateBlockState(blockState, isItemPreview);

            if (!isItem)
            {
                // Schedule an initial update
                isDirty = true;
            }
        }
    }
}