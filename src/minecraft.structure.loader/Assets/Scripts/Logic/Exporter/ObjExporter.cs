using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Logic.Exporter
{
    public class ObjExporter : IExporter
    {
        public string Extension => "obj";

        public byte[] GetMeshBytes(GameObject root, string structureName)
        {
            using var stream = new MemoryStream();
            SaveOBJToStream(root, stream, structureName);

            return stream.ToArray();
        }

        private void SaveOBJToStream(GameObject root, Stream stream, string structureName)
        {
            MeshFilter meshFilter = root.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                Debug.Log("No MeshFilter found.");
                return;
            }

            Renderer renderer = root.GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.Log("No Renderer found.");
                return;
            }

            Mesh mesh = meshFilter.sharedMesh;
            Material[] materials = renderer.sharedMaterials;

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uvs = mesh.uv;

            bool hasUVs =       uvs != null && uvs.Length > 0;
            bool hasNormals =   normals != null && normals.Length > 0;
            
            stream.Position = 0;
            using StreamWriter w = new(stream, new UTF8Encoding(false));

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                w.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "v {0} {1} {2}", v.z, v.y, v.x));
            }

            if (hasUVs)
                for (int i = 0; i < uvs.Length; i++)
                    w.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "vt {0} {1}", uvs[i].x, uvs[i].y));

            if (hasNormals)
                for (int i = 0; i < normals.Length; i++)
                    w.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "vn {0} {1} {2}", normals[i].z, normals[i].y, normals[i].x));

            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                string matName = (s < materials.Length && materials[s] != null)
                    ? materials[s].name
                    : "default";

                w.WriteLine($"\ng {structureName}_{s}");
                w.WriteLine($"usemtl {matName}");

                int[] triangles = mesh.GetTriangles(s);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int i0 = triangles[i + 0] + 1;
                    int i1 = triangles[i + 2] + 1;
                    int i2 = triangles[i + 1] + 1;

                    if (hasUVs && hasNormals)
                        w.WriteLine($"f {i0}/{i0}/{i0} {i1}/{i1}/{i1} {i2}/{i2}/{i2}");
                    else if (hasUVs)
                        w.WriteLine($"f {i0}/{i0} {i1}/{i1} {i2}/{i2}");
                    else if (hasNormals)
                        w.WriteLine($"f {i0}//{i0} {i1}//{i1} {i2}//{i2}");
                    else
                        w.WriteLine($"f {i0} {i1} {i2}");
                }
            }
        }
    }
}