using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Core.Exporter
{
    public class FbxExporter : IExporter
    {
        public string Extension => "fbx";

        public byte[] GetMeshBytes(GameObject root, string structureName)
        {
            using MemoryStream stream = new();
            SaveFBXToStream(root, stream, structureName);

            return stream.ToArray();
        }

        private static void SaveFBXToStream(GameObject root, Stream stream, string structureName)
        {
            MeshFilter meshFilter = root.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                Debug.LogWarning("No MeshFilter.");
                return;
            }

            Renderer renderer = root.GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.LogWarning("No Renderer.");
                return;
            }

            Mesh mesh = meshFilter.sharedMesh;
            Material[] materials = renderer.sharedMaterials;

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uvs = mesh.uv;
            Color[] colors = mesh.colors;

            bool hasUVs = uvs != null && uvs.Length > 0;
            bool hasNormals = normals != null && normals.Length > 0;
            bool hasColors = colors != null && colors.Length > 0;

            // Unity uses a left-handed Y-up coordinate system; FBX uses right-handed Y-up.
            // Flip X to mirror across the YZ plane so the mesh doesn't appear mirrored in DCC tools.
            static Vector3 ToFBX(Vector3 v) => new(-v.x, v.y, v.z);

            using StreamWriter w = new(stream, new UTF8Encoding(false));

            // ── FBX header ────────────────────────────────────────────────────────
            WriteHeader(w);

            // ── Global settings ───────────────────────────────────────────────────
            WriteGlobalSettings(w);

            // ── Geometry node ─────────────────────────────────────────────────────
            long geometryId = 100_000_000L;
            long modelId = 200_000_000L;
            long materialId = 300_000_000L;

            w.WriteLine(";Object definitions");
            w.WriteLine(";------------------------------------------------------------------");
            w.WriteLine();
            w.WriteLine("Definitions:  {");
            w.WriteLine("\tVersion: 100");
            w.WriteLine("\tCount: 3");
            w.WriteLine("\tObjectType: \"Geometry\" {");
            w.WriteLine("\t\tCount: 1");
            w.WriteLine("\t}");
            w.WriteLine("\tObjectType: \"Model\" {");
            w.WriteLine("\t\tCount: 1");
            w.WriteLine("\t}");
            w.WriteLine("\tObjectType: \"Material\" {");
            w.WriteLine($"\t\tCount: {mesh.subMeshCount}");
            w.WriteLine("\t}");
            w.WriteLine("}");
            w.WriteLine();

            // ── Objects section ───────────────────────────────────────────────────
            w.WriteLine("Objects:  {");

            // ── Geometry ──────────────────────────────────────────────────────────
            w.WriteLine($"\tGeometry: {geometryId}, \"{structureName}\", \"Mesh\" {{");
            w.WriteLine("\t\tVersion: 124");

            // Vertices — FBX stores them flat: X,Y,Z,X,Y,Z,...
            w.WriteLine("\t\tVertices: *" + (vertices.Length * 3) + " {");
            w.Write("\t\t\ta: ");
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = ToFBX(vertices[i]);
                if (i > 0) w.Write(",");
                w.Write(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2}", v.x, v.y, v.z));
            }

            w.WriteLine();
            w.WriteLine("\t\t}");

            // Polygon vertex index — FBX marks the last index of each polygon as ~index (bitwise NOT)
            int totalTriangles = 0;
            int[][] allTriangles = new int[mesh.subMeshCount][];
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                allTriangles[s] = mesh.GetTriangles(s);
                totalTriangles += allTriangles[s].Length;
            }

            w.WriteLine("\t\tPolygonVertexIndex: *" + totalTriangles + " {");
            w.Write("\t\t\ta: ");
            bool firstIdx = true;
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                int[] tris = allTriangles[s];
                for (int i = 0; i < tris.Length; i += 3)
                {
                    if (!firstIdx) w.Write(",");
                    firstIdx = false;
                    // Winding order: flip to compensate for X-axis flip
                    w.Write(string.Format(CultureInfo.InvariantCulture,
                        "{0},{1},{2}", tris[i + 0], tris[i + 2], ~tris[i + 1]));
                }
            }

            w.WriteLine();
            w.WriteLine("\t\t}");

            // Geometry version
            w.WriteLine("\t\tGeometryVersion: 124");

            // Layer element: normals
            if (hasNormals)
            {
                w.WriteLine("\t\tLayerElementNormal: 0 {");
                w.WriteLine("\t\t\tVersion: 102");
                w.WriteLine("\t\t\tName: \"\"");
                w.WriteLine("\t\t\tMappingInformationType: \"ByPolygonVertex\"");
                w.WriteLine("\t\t\tReferenceInformationType: \"Direct\"");
                w.WriteLine("\t\t\tNormals: *" + (totalTriangles * 3) + " {");
                w.Write("\t\t\t\ta: ");
                bool firstNrm = true;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    int[] tris = allTriangles[s];
                    for (int i = 0; i < tris.Length; i += 3)
                    {
                        // Match the flipped winding order: 0, 2, 1
                        int[] order = { tris[i], tris[i + 2], tris[i + 1] };
                        foreach (int idx in order)
                        {
                            if (!firstNrm) w.Write(",");
                            firstNrm = false;
                            Vector3 n = ToFBX(normals[idx]);
                            w.Write(string.Format(CultureInfo.InvariantCulture,
                                "{0},{1},{2}", n.x, n.y, n.z));
                        }
                    }
                }

                w.WriteLine();
                w.WriteLine("\t\t\t}");
                w.WriteLine("\t\t}");
            }

            // Layer element: UVs
            if (hasUVs)
            {
                w.WriteLine("\t\tLayerElementUV: 0 {");
                w.WriteLine("\t\t\tVersion: 101");
                w.WriteLine("\t\t\tName: \"UVSet0\"");
                w.WriteLine("\t\t\tMappingInformationType: \"ByPolygonVertex\"");
                w.WriteLine("\t\t\tReferenceInformationType: \"IndexToDirect\"");

                // UV array (all unique UVs)
                w.WriteLine("\t\t\tUV: *" + (uvs.Length * 2) + " {");
                w.Write("\t\t\t\ta: ");
                for (int i = 0; i < uvs.Length; i++)
                {
                    if (i > 0) w.Write(",");
                    w.Write(string.Format(CultureInfo.InvariantCulture,
                        "{0},{1}", uvs[i].x, uvs[i].y));
                }

                w.WriteLine();
                w.WriteLine("\t\t\t}");

                // UV indices — one per polygon vertex
                w.WriteLine("\t\t\tUVIndex: *" + totalTriangles + " {");
                w.Write("\t\t\t\ta: ");
                bool firstUV = true;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    int[] tris = allTriangles[s];
                    for (int i = 0; i < tris.Length; i += 3)
                    {
                        int[] order = { tris[i], tris[i + 2], tris[i + 1] };
                        foreach (int idx in order)
                        {
                            if (!firstUV) w.Write(",");
                            firstUV = false;
                            w.Write(idx.ToString(CultureInfo.InvariantCulture));
                        }
                    }
                }

                w.WriteLine();
                w.WriteLine("\t\t\t}");
                w.WriteLine("\t\t}");
            }

            if (hasColors)
            {
                w.WriteLine("\t\tLayerElementColor: 0 {");
                w.WriteLine("\t\t\tVersion: 101");
                w.WriteLine("\t\t\tName: \"VertexColors\"");
                w.WriteLine("\t\t\tMappingInformationType: \"ByPolygonVertex\"");
                w.WriteLine("\t\t\tReferenceInformationType: \"IndexToDirect\"");

                // RGBA color array (all unique vertex colors)
                w.WriteLine("\t\t\tColors: *" + (colors.Length * 4) + " {");
                w.Write("\t\t\t\ta: ");
                for (int i = 0; i < colors.Length; i++)
                {
                    if (i > 0) w.Write(",");
                    w.Write(string.Format(CultureInfo.InvariantCulture,
                        "{0},{1},{2},{3}", colors[i].r, colors[i].g, colors[i].b, colors[i].a));
                }

                w.WriteLine();
                w.WriteLine("\t\t\t}");

                // One index per polygon vertex (same order as UV indices)
                w.WriteLine("\t\t\tColorIndex: *" + totalTriangles + " {");
                w.Write("\t\t\t\ta: ");
                bool firstCol = true;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    int[] tris = allTriangles[s];
                    for (int i = 0; i < tris.Length; i += 3)
                    {
                        int[] order = { tris[i], tris[i + 2], tris[i + 1] };
                        foreach (int idx in order)
                        {
                            if (!firstCol) w.Write(",");
                            firstCol = false;
                            w.Write(idx.ToString(CultureInfo.InvariantCulture));
                        }
                    }
                }

                w.WriteLine();
                w.WriteLine("\t\t\t}");
                w.WriteLine("\t\t}");
            }

            // Layer element: material assignment (one material index per polygon)
            w.WriteLine("\t\tLayerElementMaterial: 0 {");
            w.WriteLine("\t\t\tVersion: 101");
            w.WriteLine("\t\t\tName: \"\"");
            w.WriteLine("\t\t\tMappingInformationType: \"ByPolygon\"");
            w.WriteLine("\t\t\tReferenceInformationType: \"IndexToDirect\"");
            int polyCount = totalTriangles / 3;
            w.WriteLine("\t\t\tMaterials: *" + polyCount + " {");
            w.Write("\t\t\t\ta: ");
            bool firstMat = true;
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                int polyCountInSub = allTriangles[s].Length / 3;
                for (int p = 0; p < polyCountInSub; p++)
                {
                    if (!firstMat) w.Write(",");
                    firstMat = false;
                    w.Write(s.ToString(CultureInfo.InvariantCulture));
                }
            }

            w.WriteLine();
            w.WriteLine("\t\t\t}");
            w.WriteLine("\t\t}");

            // Layer descriptor
            w.WriteLine("\t\tLayer: 0 {");
            w.WriteLine("\t\t\tVersion: 100");
            w.WriteLine("\t\t\tLayerElement: {");
            w.WriteLine("\t\t\t\tType: \"LayerElementNormal\"");
            w.WriteLine("\t\t\t\tTypedIndex: 0");
            w.WriteLine("\t\t\t}");
            if (hasUVs)
            {
                w.WriteLine("\t\t\tLayerElement: {");
                w.WriteLine("\t\t\t\tType: \"LayerElementUV\"");
                w.WriteLine("\t\t\t\tTypedIndex: 0");
                w.WriteLine("\t\t\t}");
            }

            if (hasColors)
            {
                w.WriteLine("\t\t\tLayerElement: {");
                w.WriteLine("\t\t\t\tType: \"LayerElementColor\"");
                w.WriteLine("\t\t\t\tTypedIndex: 0");
                w.WriteLine("\t\t\t}");
            }

            w.WriteLine("\t\t\tLayerElement: {");
            w.WriteLine("\t\t\t\tType: \"LayerElementMaterial\"");
            w.WriteLine("\t\t\t\tTypedIndex: 0");
            w.WriteLine("\t\t\t}");
            w.WriteLine("\t\t}");
            w.WriteLine("\t}"); // end Geometry

            // ── Model node ────────────────────────────────────────────────────────
            w.WriteLine($"\tModel: {modelId}, \"{structureName}\", \"Mesh\" {{");
            w.WriteLine("\t\tVersion: 232");
            w.WriteLine("\t\tProperties70:  {");
            w.WriteLine("\t\t\tP: \"RotationActive\", \"bool\", \"\", \"\",1");
            w.WriteLine("\t\t\tP: \"InheritType\", \"enum\", \"\", \"\",1");
            w.WriteLine("\t\t\tP: \"ScalingMax\", \"Vector3D\", \"Vector\", \"\",0,0,0");
            w.WriteLine("\t\t\tP: \"DefaultAttributeIndex\", \"int\", \"Integer\", \"\",0");
            w.WriteLine("\t\t}");
            w.WriteLine("\t\tShading: T");
            w.WriteLine("\t\tCulling: \"CullingOff\"");
            w.WriteLine("\t}"); // end Model

            // ── Material nodes ────────────────────────────────────────────────────
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                string matName = (s < materials.Length && materials[s] != null)
                    ? materials[s].name
                    : "default";

                long matId = materialId + s;
                w.WriteLine($"\tMaterial: {matId}, \"{matName}\", \"\" {{");
                w.WriteLine("\t\tVersion: 102");
                w.WriteLine("\t\tShadingModel: \"phong\"");
                w.WriteLine("\t\tMultiLayer: 0");
                w.WriteLine("\t\tProperties70:  {");
                w.WriteLine("\t\t\tP: \"AmbientColor\", \"Color\", \"\", \"A\",0,0,0");
                w.WriteLine("\t\t\tP: \"DiffuseColor\", \"Color\", \"\", \"A\",0.8,0.8,0.8");
                w.WriteLine("\t\t\tP: \"SpecularColor\", \"Color\", \"\", \"A\",0.2,0.2,0.2");
                w.WriteLine("\t\t\tP: \"Shininess\", \"double\", \"Number\", \"\",20");
                w.WriteLine("\t\t\tP: \"Opacity\", \"double\", \"Number\", \"\",1");
                w.WriteLine("\t\t}");
                w.WriteLine("\t}"); // end Material
            }

            w.WriteLine("}"); // end Objects
            w.WriteLine();

            // ── Connections ───────────────────────────────────────────────────────
            w.WriteLine("Connections:  {");
            // Geometry -> Model
            w.WriteLine($"\tC: \"OO\",{geometryId},{modelId}");
            // Model -> root (scene)
            w.WriteLine($"\tC: \"OO\",{modelId},0");
            // Materials -> Model
            for (int s = 0; s < mesh.subMeshCount; s++)
                w.WriteLine($"\tC: \"OO\",{materialId + s},{modelId}");
            w.WriteLine("}");
            w.WriteLine();
        }

        private static void WriteHeader(StreamWriter w)
        {
            w.WriteLine("; FBX 7.7.0 project file");
            w.WriteLine("; Created by SNIGUR's FBXExporter tool");
            w.WriteLine("; ----------------------------------------------------");
            w.WriteLine();
            w.WriteLine("FBXHeaderExtension:  {");
            w.WriteLine("\tFBXHeaderVersion: 1004");
            w.WriteLine("\tFBXVersion: 7700");
            w.WriteLine($"\tCreationTimeStamp:  {{");
            w.WriteLine($"\t\tVersion: 1000");
            w.WriteLine($"\t\tYear: {DateTime.Now.Year}");
            w.WriteLine($"\t\tMonth: {DateTime.Now.Month}");
            w.WriteLine($"\t\tDay: {DateTime.Now.Day}");
            w.WriteLine($"\t\tHour: {DateTime.Now.Hour}");
            w.WriteLine($"\t\tMinute: {DateTime.Now.Minute}");
            w.WriteLine($"\t\tSecond: {DateTime.Now.Second}");
            w.WriteLine("\t\tMillisecond: 0");
            w.WriteLine("\t}");
            w.WriteLine("\tCreator: \"SNIGUR's FBXExporter\"");
            w.WriteLine("}");
            w.WriteLine();
        }

        private static void WriteGlobalSettings(StreamWriter w)
        {
            w.WriteLine("GlobalSettings:  {");
            w.WriteLine("\tVersion: 1000");
            w.WriteLine("\tProperties70:  {");
            w.WriteLine("\t\tP: \"UpAxis\", \"int\", \"Integer\", \"\",1"); // Y-up
            w.WriteLine("\t\tP: \"UpAxisSign\", \"int\", \"Integer\", \"\",1");
            w.WriteLine("\t\tP: \"FrontAxis\", \"int\", \"Integer\", \"\",2"); // Z-front
            w.WriteLine("\t\tP: \"FrontAxisSign\", \"int\", \"Integer\", \"\",1");
            w.WriteLine("\t\tP: \"CoordAxis\", \"int\", \"Integer\", \"\",0"); // X-right
            w.WriteLine("\t\tP: \"CoordAxisSign\", \"int\", \"Integer\", \"\",1");
            w.WriteLine("\t\tP: \"UnitScaleFactor\", \"double\", \"Number\", \"\",100");
            w.WriteLine("\t\tP: \"AmbientColor\", \"ColorRGB\", \"Color\", \"\",0,0,0");
            w.WriteLine("\t}");
            w.WriteLine("}");
            w.WriteLine();
        }
    }
}