using System.Collections.Generic;
using Core.Utils;
using CraftSharp;
using UnityEngine;

namespace Core
{
    public class StructureVolume
    {
        private const byte CULL_NONE = 0b111111;
        private const byte CULL_UP = 0b111110;
        private const byte CULL_DOWN = 0b111101;
        private const byte CULL_FORWARD = 0b101111;
        private const byte CULL_BACK = 0b011111;
        private const byte CULL_LEFT = 0b110111;
        private const byte CULL_RIGHT = 0b111011;

        private readonly Dictionary<Vector3, BlockState> _volumeData = new();

        public BlockState this[float x, float y, float z]
        {
            get => this[new Vector3(x, y, z)];
            set => this[new Vector3(x, y, z)] = value;
        }
    
        public BlockState this[Vector3 pos]
        {
            get => _volumeData.GetValueOrDefault(pos, BlockState.AIR_STATE);
            set => _volumeData[pos] = value;
        }

        public void Reset() =>
            _volumeData.Clear();

        public byte GetCullFlagsFor(Vector3 pos)
        {
            if (!IsCullableAt(pos))
                return CULL_NONE;

            byte flags = CULL_NONE;

            AddCullFlag(ref flags, pos, pos.Up(),      CULL_UP);
            AddCullFlag(ref flags, pos, pos.Down(),    CULL_DOWN);
            AddCullFlag(ref flags, pos, pos.Left(),    CULL_LEFT);
            AddCullFlag(ref flags, pos, pos.Right(),   CULL_RIGHT);
            AddCullFlag(ref flags, pos, pos.Forward(), CULL_FORWARD);
            AddCullFlag(ref flags, pos, pos.Back(),    CULL_BACK);

            return flags;
        }

        private void AddCullFlag(ref byte flags, Vector3 origin, Vector3 neighbor, byte flag)
        {
            if (IsCullableAt(neighbor) && !IsNeighborTranslucent(origin, neighbor))
                flags &= flag;
        }

        private bool IsNeighborTranslucent(Vector3 origin, Vector3 neighbor)
        {
            return _volumeData.TryGetValue(origin, out var originState)
                   && _volumeData.TryGetValue(neighbor, out var neighborState)
                   && BlockStateHelper.GetRenderTypeFor(originState.BlockId.Path) == RenderType.SOLID
                   && BlockStateHelper.GetRenderTypeFor(neighborState.BlockId.Path) == RenderType.TRANSLUCENT;
        }
        
        private bool IsCullableAt(Vector3 pos)
        {
            if (!_volumeData.TryGetValue(pos, out var state))
                return false;

            RenderType renderType = BlockStateHelper.GetRenderTypeFor(state.BlockId.Path);

            return renderType switch
            {
                RenderType.TRANSLUCENT => BlockStateHelper.IsTranslucentCullable(state.BlockId.Path),
                RenderType.SOLID => !state.IsLava && !BlockStateHelper.IsSemiSolid(state.BlockId.Path),
                _ => false
            };
        }
    }
}