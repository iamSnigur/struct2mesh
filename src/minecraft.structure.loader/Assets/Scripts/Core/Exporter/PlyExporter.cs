using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Core.Exporter
{
    public class PlyExporter : IExportStrategy
    {
        public string Extension => "ply";

        public byte[] GetMeshBytes(GameObject root, string structureName)
        {
            using MemoryStream stream = new();
            SavePLYToStream(root, stream);
                
            return stream.ToArray();
        }

        private void SavePLYToStream(GameObject root, Stream stream)
        {
            MeshFilter meshFilter = root.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                Debug.Log("No MeshFilter found.");
                return;
            }

            Mesh mesh = meshFilter.sharedMesh;

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uvs = mesh.uv;
            Color[] colors = mesh.colors;

            bool hasNormals = normals != null && normals.Length == vertices.Length;
            bool hasUVs = uvs != null && uvs.Length == vertices.Length;
            bool hasColors = colors != null && colors.Length == vertices.Length;

            int faceCount = 0;
            for (int s = 0; s < mesh.subMeshCount; s++)
                faceCount += mesh.GetTriangles(s).Length / 3;

            stream.Position = 0;

            using StreamWriter w = new(stream, new UTF8Encoding(false));

            // Header
            w.WriteLine("ply");
            w.WriteLine("format ascii 1.0");

            w.WriteLine($"element vertex {vertices.Length}");

            w.WriteLine("property float x");
            w.WriteLine("property float y");
            w.WriteLine("property float z");

            if (hasNormals)
            {
                w.WriteLine("property float nx");
                w.WriteLine("property float ny");
                w.WriteLine("property float nz");
            }

            if (hasUVs)
            {
                w.WriteLine("property float s");
                w.WriteLine("property float t");
            }

            if (hasColors)
            {
                w.WriteLine("property uchar red");
                w.WriteLine("property uchar green");
                w.WriteLine("property uchar blue");
                w.WriteLine("property uchar alpha");
            }

            w.WriteLine($"element face {faceCount}");
            w.WriteLine("property list uchar int vertex_indices");
            w.WriteLine("end_header");

            // Vertices
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];

                StringBuilder line = new();

                // Same coordinate swap as your OBJ exporter
                line.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "{0} {1} {2}",
                    v.z, v.y, v.x);

                if (hasNormals)
                {
                    Vector3 n = normals[i];
                    line.AppendFormat(
                        CultureInfo.InvariantCulture,
                        " {0} {1} {2}",
                        n.z, n.y, n.x);
                }

                if (hasUVs)
                {
                    Vector2 uv = uvs[i];
                    line.AppendFormat(
                        CultureInfo.InvariantCulture,
                        " {0} {1}",
                        uv.x, uv.y);
                }

                if (hasColors)
                {
                    Color32 c = colors[i];
                    line.AppendFormat(
                        CultureInfo.InvariantCulture,
                        " {0} {1} {2} {3}",
                        c.r, c.g, c.b, c.a);
                }

                w.WriteLine(line.ToString());
            }

            // Faces
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                int[] triangles = mesh.GetTriangles(s);

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    // Match your flipped winding order
                    int i0 = triangles[i + 0];
                    int i1 = triangles[i + 2];
                    int i2 = triangles[i + 1];

                    w.WriteLine($"3 {i0} {i1} {i2}");
                }
            }
        }
    }
}