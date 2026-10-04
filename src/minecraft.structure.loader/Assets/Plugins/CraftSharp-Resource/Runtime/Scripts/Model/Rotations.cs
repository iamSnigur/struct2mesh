using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace CraftSharp.Resource
{
    public static class Rotations
    {
        // rotation used in model texture uvs
        public enum UVRot
        {
            UV_0, UV_90, UV_180, UV_270
        }

        // Minecraft Z, Y ,X axis
        // X and Z axis are swapped for Unity use
        public enum Axis
        {
            X, Y, Z
        }

        // By default unity uses z-x-y order for euler angles, while
        // we use x-y-z order for our bedrock entity models, so we'll
        // do a bit conversion here
        public static Quaternion RotationFromEulersXYZ(float3 eularsXYZ)
        {
            return Quaternion.AngleAxis(eularsXYZ.x, Vector3.right) *
                    Quaternion.AngleAxis(eularsXYZ.y, Vector3.up) *
                    Quaternion.AngleAxis(eularsXYZ.z, Vector3.forward);
        }

        /// <summary>
        /// Rotates vertices around a pivot point on multiple axes (X, Y, Z) sequentially.
        /// Applies non-zero rotations only, in X → Y → Z order.
        /// </summary>
        /// <param name="verts">Vertices to rotate in-place</param>
        /// <param name="pivot">Pivot point in world space (pre-scaled)</param>
        /// <param name="angles">Rotation angles in degrees for each axis (X, Y, Z)</param>
        /// <param name="rescale">Whether to rescale vertices after rotation (matches single-axis behavior)</param>
        public static void RotateVerticesXYZ(ref float3[] verts, float3 pivot, float3 angles, bool rescale)
        {
            // Apply each axis independently, matching single-axis RotateVertices behavior.
            // Order: X → Y → Z (Minecraft's convention for multi-axis element rotation).
            // Swaped X and Z just because it works that way
            if (angles.x != 0f)
                RotateVertices(ref verts, pivot, Axis.Z, angles.x, rescale);

            if (angles.y != 0f)
                RotateVertices(ref verts, pivot, Axis.Y, angles.y, rescale);

            if (angles.z != 0f)
                RotateVertices(ref verts, pivot, Axis.X, angles.z, rescale);
        }
        
        // For Bedrock Edition Entity models
        public static void RotateVertices(ref float3[] original, float3 pivot, float3 eularsXYZ, float downScale = 1F, int startIndex = 0)
        {
            // Set up rotation quaternion...
            Quaternion rot = RotationFromEulersXYZ(eularsXYZ);

            // And rotate vertices...
            for (int i = startIndex;i < original.Length;i++)
            {
                var offset = original[i] - pivot;

                if (downScale == 1F)
                {
                    original[i] = (float3)(rot * offset) + pivot;
                }
                else
                {
                    original[i] = ((float3)(rot * offset) + pivot) / downScale;
                }
            }
        }

        // For Java Edition Block/Item models
        public static void RotateVertices(ref float3[] original, float3 pivot, Axis axis, float degree, bool rescale)
        {
            // Set up rotation quaternion...
            Quaternion rot = axis switch
            {
                Axis.X => Quaternion.Euler(degree, 0F, 0F),
                Axis.Y => Quaternion.Euler(0F, degree, 0F),
                Axis.Z => Quaternion.Euler(0F, 0F, degree),
                _      => Quaternion.identity
            };

            // And rotate vertices...
            for (int i = 0;i < original.Length;i++)
            {
                var offset = original[i] - pivot;
                if (rescale)
                {
                    var scaleFrac = 1F / Mathf.Cos(Mathf.Deg2Rad * degree);
                    switch (axis)
                    {
                        case Axis.X:
                            offset.y *= scaleFrac;
                            offset.z *= scaleFrac;
                            break;
                        case Axis.Y:
                            offset.x *= scaleFrac;
                            offset.z *= scaleFrac;
                            break;
                        case Axis.Z:
                            offset.x *= scaleFrac;
                            offset.y *= scaleFrac;
                            break;
                    }
                }
                original[i] = (float3)(rot * offset) + pivot; // TODO: Make this more efficient
            }
        }

        private static float3 ROTCENTER = new(0.5F, 0.5F, 0.5F);

        // For Java Edition Blockstate models
        public static void RotateWrapper(ref float3[] original, int2 zyRot)
        {
            // Set up rotation quaternion...
            Quaternion rot = Quaternion.Euler(0F, zyRot.y * 22.5F, zyRot.x * 90F);

            // And rotate vertices...
            for (int i = 0;i < original.Length;i++)
            {
                original[i] = (float3)(rot * (original[i] - ROTCENTER)) + ROTCENTER; // TODO Make this better
            }
        }
    }
}
