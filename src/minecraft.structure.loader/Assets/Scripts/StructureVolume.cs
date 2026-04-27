using System.Collections.Generic;
using CraftSharp;
using UnityEngine;

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
        if (!IsBlockSolidAt(pos))
            return CULL_NONE;

        byte cullFlags = CULL_NONE;

        if (IsBlockSolidAt(pos.Up()))
            cullFlags &= CULL_UP;

        if (IsBlockSolidAt(pos.Down()))
            cullFlags &= CULL_DOWN;

        if (IsBlockSolidAt(pos.Left()))
            cullFlags &= CULL_LEFT;

        if (IsBlockSolidAt(pos.Right()))
            cullFlags &= CULL_RIGHT;

        if (IsBlockSolidAt(pos.Forward()))
            cullFlags &= CULL_FORWARD;

        if (IsBlockSolidAt(pos.Back()))
            cullFlags &= CULL_BACK;

        return cullFlags;
    }

    private bool IsBlockSolidAt(Vector3 pos)
    {
        if (!_volumeData.TryGetValue(pos, out BlockState state))
            return false;

        RenderType renderType = BlockStateHelper.GetRenderTypeFor(state.BlockId.Path);

        if (renderType is not RenderType.SOLID)
            return false;

        return !BlockStateHelper.IsSemiSolid(state.BlockId.Path);
    }
}