using System.Collections.Generic;
using UnityEngine;
using CraftSharp.Resource;

public static class MinecraftMeshBuilder
{
    // Each Minecraft face is a quad = 4 verts, 2 triangles (6 indices)
    // Face vertex order (CCW from outside): BL, TL, TR, BR
    // Unity uses clockwise winding, so we flip to: BL, TR, TL  +  BL, BR, TR

    public static Mesh BuildMesh(JsonModel model, Texture2D atlasTexture = null)
    {
        var verts   = new List<Vector3>();
        var uvs     = new List<Vector2>();
        var tris    = new List<int>();
        var normals = new List<Vector3>();

        foreach (JsonModelElement element in model.Elements)
        {
            // from/to are in Minecraft units (0–16), convert to Unity units (0–1)
            Vector3 from = new Vector3(element.from.x, element.from.y, element.from.z) / 16f;
            Vector3 to   = new Vector3(element.to.x,   element.to.y,   element.to.z)   / 16f;

            foreach ((FaceDir dir, BlockModelFace face) in element.faces)
            {
                AddFace(verts, uvs, tris, normals, from, to, dir, face, atlasTexture);
            }
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.SetNormals(normals);
        //mesh.RecalculateBounds();
        //mesh.RecalculateNormals();
        return mesh;
    }

    static void AddFace(
        List<Vector3> verts, List<Vector2> uvs,
        List<int> tris, List<Vector3> normals,
        Vector3 from, Vector3 to,
        FaceDir dir, BlockModelFace face,
        Texture2D atlas)
    {
        int baseIdx = verts.Count;

        // Build 4 corners of the quad based on face direction
        Vector3[] corners = GetFaceCorners(from, to, dir);
        Vector3   normal  = GetFaceNormal(dir);

        // Convert Minecraft UV [0-16] to Unity UV [0-1]
        // IMPORTANT: flip V because MC is top-left origin, Unity is bottom-left
        float u0 = face.uv[0] / 16f;
        float v0 = 1f - face.uv[1] / 16f;  // flip V
        float u1 = face.uv[2] / 16f;
        float v1 = 1f - face.uv[3] / 16f;  // flip V

        // If the model uses a texture atlas, remap into atlas space here
        // (skip for now if using per-model textures)

        // Handle face rotation (0, 90, 180, 270 degrees)
        Vector2[] faceUVs = ApplyUVRotation(
            new Vector2(u0, v1), new Vector2(u0, v0),  // BL, TL
            new Vector2(u1, v0), new Vector2(u1, v1),  // TR, BR
            face.rot);

        for (int i = 0; i < 4; i++)
        {
            verts.Add(corners[i]);
            uvs.Add(faceUVs[i]);
            normals.Add(normal);
        }

        // Two triangles: BL-TL-TR and BL-TR-BR (clockwise in Unity)
        tris.Add(baseIdx);     tris.Add(baseIdx + 2); tris.Add(baseIdx + 1);
        tris.Add(baseIdx);     tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
    }

    static Vector3[] GetFaceCorners(Vector3 from, Vector3 to, FaceDir dir)
    {
        // Returns 4 corners: BL, TL, TR, BR (viewed from outside)
        return dir switch
        {
            FaceDir.UP    => new[] { new Vector3(to.x,   to.y, from.z),
                                        new Vector3(to.x,   to.y, to.z),
                                        new Vector3(from.x, to.y, to.z),
                                        new Vector3(from.x, to.y, from.z) },
            FaceDir.DOWN  => new[] { new Vector3(to.x,   from.y, to.z),
                                        new Vector3(to.x,   from.y, from.z),
                                        new Vector3(from.x, from.y, from.z),
                                        new Vector3(from.x, from.y, to.z) },
            FaceDir.NORTH => new[] { new Vector3(to.x,   from.y, from.z),
                                        new Vector3(to.x,   to.y,   from.z),
                                        new Vector3(from.x, to.y,   from.z),
                                        new Vector3(from.x, from.y, from.z) },
            FaceDir.SOUTH => new[] { new Vector3(from.x, from.y, to.z),
                                        new Vector3(from.x, to.y,   to.z),
                                        new Vector3(to.x,   to.y,   to.z),
                                        new Vector3(to.x,   from.y, to.z) },
            FaceDir.EAST  => new[] { new Vector3(to.x, from.y, to.z),
                                        new Vector3(to.x, to.y,   to.z),
                                        new Vector3(to.x, to.y,   from.z),
                                        new Vector3(to.x, from.y, from.z) },
            FaceDir.WEST  => new[] { new Vector3(from.x, from.y, from.z),
                                        new Vector3(from.x, to.y,   from.z),
                                        new Vector3(from.x, to.y,   to.z),
                                        new Vector3(from.x, from.y, to.z) },
            _ => throw new System.ArgumentException($"Unknown face: {dir}")
        };
    }

    static Vector3 GetFaceNormal(FaceDir dir) => dir switch
    {
        FaceDir.UP    => Vector3.up,
        FaceDir.DOWN  => Vector3.down,
        FaceDir.NORTH => Vector3.back,
        FaceDir.SOUTH => Vector3.forward,
        FaceDir.EAST  => Vector3.right,
        FaceDir.WEST  => Vector3.left,
        _ => Vector3.zero
    };

    static Vector2[] ApplyUVRotation(Vector2 bl, Vector2 tl, Vector2 tr, Vector2 br, Rotations.UVRot rotationUV)
    {
        // rotation is 0, 90, 180, 270 — rotates the UV quad
        Vector2[] uvs = { bl, tl, tr, br };

        int rotation = 
            rotationUV switch
            {
                Rotations.UVRot.UV_0 => 0,
                Rotations.UVRot.UV_90 => 90,
                Rotations.UVRot.UV_180 => 180,
                _ => 270,
            };
        
        int steps = (rotation / 90) % 4;
        for (int i = 0; i < steps; i++)
        {
            // Rotate 90° CW: BL→TL→TR→BR→BL
            var tmp = uvs[0];
            uvs[0] = uvs[1]; uvs[1] = uvs[2];
            uvs[2] = uvs[3]; uvs[3] = tmp;
        }
        return uvs;
    }
}