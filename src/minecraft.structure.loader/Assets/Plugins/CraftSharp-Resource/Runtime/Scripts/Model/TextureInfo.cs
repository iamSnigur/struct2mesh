using System;
using UnityEngine;

namespace CraftSharp.Resource
{
    [Serializable]
    public struct TextureInfo
    {
        // Workaround to fix texture bleeding issue on block edges
        // Offset UVs inwards
        private const float BLEEDING_OFFSET = 0.02f / ResourcePackManager.ATLAS_SIZE;
        
        public Rect bounds;
        public int index;

        // More than 1 if the texture is animated
        public int frameCount;
        // Only used if it is an animated texture
        public float frameSize;
        public int framePerRow;
        public bool interpolate;
        public float frameInterval; // Duration of each frame

        public TextureInfo(Rect bounds, int index, int fc = 1, float fs = 1, int fr = 1, bool itpl = false, float fi = 1)
        {
            this.bounds = bounds;
            
            this.bounds.xMin += BLEEDING_OFFSET;
            this.bounds.yMin += BLEEDING_OFFSET;
            this.bounds.xMax -= BLEEDING_OFFSET;
            this.bounds.yMax -= BLEEDING_OFFSET;
            
            this.index = index;
            this.frameCount = fc;
            this.frameSize = fs;
            this.framePerRow = fr;
            this.interpolate = itpl;
            this.frameInterval = fi;
        }
    }
}