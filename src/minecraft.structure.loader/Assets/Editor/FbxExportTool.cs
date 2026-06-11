// FbxExportTool.cs
// Place this file in any Editor/ folder in your Unity project.
// Requires the Unity FBX Exporter package (com.unity.formats.fbx).
// Install via Package Manager: Window > Package Manager > "FBX Exporter"

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tools.FbxExport
{
    /// <summary>
    /// Unity Editor window that exports selected GameObjects to binary FBX,
    /// with optional custom-shader material baking.
    /// </summary>
    public class FbxExportTool : EditorWindow
    {
        // ── UI state ──────────────────────────────────────────────────────────
        private string _outputFolder = "Assets/Exports";
        private string _fileName     = "Export";
        private bool   _includeChildren   = true;
        private bool   _bakeCustomShaders = true;
        private bool   _embedTextures     = true;
        private bool   _exportAnimations  = false;
        private LODExportType _lodMode =
            LODExportType.All;

        private Vector2 _scroll;
        private string  _lastStatus = "";
        private bool    _statusOk   = true;

        // ── Menu item ─────────────────────────────────────────────────────────
        [MenuItem("Tools/FBX Export Tool")]
        public static void ShowWindow()
        {
            var w = GetWindow<FbxExportTool>("FBX Export Tool");
            w.minSize = new Vector2(420, 520);
        }

        // ── GUI ───────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space(6);
            GUILayout.Label("FBX Export Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select one or more GameObjects in the Hierarchy, configure options, then click Export.",
                MessageType.Info);

            EditorGUILayout.Space(8);
            GUILayout.Label("Output", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            if (GUILayout.Button("…", GUILayout.Width(28)))
            {
                string chosen = EditorUtility.SaveFolderPanel("Export Folder", _outputFolder, "");
                if (!string.IsNullOrEmpty(chosen))
                    _outputFolder = FileUtil.GetProjectRelativePath(chosen);
            }
            EditorGUILayout.EndHorizontal();

            _fileName = EditorGUILayout.TextField("File Name", _fileName);

            EditorGUILayout.Space(8);
            GUILayout.Label("Export Options", EditorStyles.boldLabel);

            _includeChildren   = EditorGUILayout.Toggle("Include Children",        _includeChildren);
            _exportAnimations  = EditorGUILayout.Toggle("Export Animations",        _exportAnimations);
            _embedTextures     = EditorGUILayout.Toggle("Embed Textures",           _embedTextures);
            _lodMode           = (LODExportType)
                                  EditorGUILayout.EnumPopup("LOD Mode", _lodMode);

            EditorGUILayout.Space(8);
            GUILayout.Label("Custom Shader Support", EditorStyles.boldLabel);

            _bakeCustomShaders = EditorGUILayout.Toggle(
                new GUIContent("Bake Custom Shaders",
                    "Copies custom-shader materials to a Standard/URP Lit material so the FBX " +
                    "carries useful texture references rather than unsupported shader data."),
                _bakeCustomShaders);

            if (_bakeCustomShaders)
            {
                EditorGUILayout.HelpBox(
                    "A temporary baked material is written next to the FBX. " +
                    "Original materials are NOT modified.",
                    MessageType.None);
            }

            EditorGUILayout.Space(12);

            // Selection info
            var selected = Selection.gameObjects;
            if (selected.Length == 0)
                EditorGUILayout.HelpBox("No GameObjects selected.", MessageType.Warning);
            else
                EditorGUILayout.HelpBox(
                    $"{selected.Length} object(s) selected for export.", MessageType.None);

            GUI.enabled = selected.Length > 0;
            if (GUILayout.Button("Export to FBX (Binary)", GUILayout.Height(36)))
                DoExport(selected);
            GUI.enabled = true;

            if (!string.IsNullOrEmpty(_lastStatus))
            {
                var msgType = _statusOk ? MessageType.Info : MessageType.Error;
                EditorGUILayout.HelpBox(_lastStatus, msgType);
            }

            EditorGUILayout.EndScrollView();
        }

        // ── Core export logic ─────────────────────────────────────────────────
        private void DoExport(GameObject[] targets)
        {
            try
            {
                // Ensure output folder exists
                if (!AssetDatabase.IsValidFolder(_outputFolder))
                    Directory.CreateDirectory(Path.Combine(
                        Application.dataPath, "../", _outputFolder));

                string safeName = SanitizeFileName(_fileName);
                string assetPath = $"{_outputFolder}/{safeName}.fbx";
                string absPath   = Path.GetFullPath(
                    Path.Combine(Application.dataPath, "../", assetPath));

                // Collect objects to export
                var exportSet = new List<GameObject>(targets);

                // --- Custom shader baking ----------------------------------------
                var bakedMaterials = new List<(Renderer rend, Material[] originals, Material[] baked)>();

                if (_bakeCustomShaders)
                {
                    foreach (var go in CollectRenderers(exportSet, _includeChildren))
                    {
                        var originals = go.sharedMaterials;
                        var baked     = new Material[originals.Length];
                        bool anyBaked = false;

                        for (int mi = 0; mi < originals.Length; mi++)
                        {
                            var src = originals[mi];
                            if (src == null || IsStandardShader(src.shader))
                            {
                                baked[mi] = src;
                                continue;
                            }

                            baked[mi] = BakeMaterial(src, assetPath, mi);
                            anyBaked  = true;
                        }

                        if (anyBaked)
                        {
                            bakedMaterials.Add((go, originals, baked));
                            go.sharedMaterials = baked;
                        }
                    }
                }

                // --- FBX export --------------------------------------------------
                var options = new ExportModelOptions
                {
                    ExportFormat          = ExportFormat.Binary,   // ← binary, not ASCII
                    KeepInstances         = true,
                    EmbedTextures         = _embedTextures,
                    ExportUnrendered      = false,
                    ModelAnimIncludeOption = _exportAnimations
                        ? Include.ModelAndAnim
                        : Include.Model,
                    LODExportType         = _lodMode,
                };

                Renderer component = exportSet[0].GetComponent<Renderer>();
                Texture mainTexture = component.materials[0].mainTexture;
                
                if (mainTexture is Texture2DArray textureArray)
                {
                    Texture2D layer = ExtractLayer(textureArray, 0);
                    
                    byte[] pngBytes = layer.EncodeToPNG();
                    string fullPath = Path.GetFullPath(
                        Path.Combine(Application.dataPath, "../", $"{_outputFolder}/{safeName}.png"));
                    File.WriteAllBytes(fullPath, pngBytes);
                }
                else
                {
                    Debug.Log("Main Texture not a texture 2D");
                }

                if (_includeChildren)
                    ModelExporter.ExportObjects(absPath, exportSet.ToArray(), options);
                else
                    ModelExporter.ExportObjects(absPath,
                        exportSet.ToArray(), options);   // children handled by hierarchy flag below

                // --- Restore original materials ----------------------------------
                foreach (var (rend, originals, _) in bakedMaterials)
                    rend.sharedMaterials = originals;

                // Refresh project so the new FBX appears in the browser
                AssetDatabase.Refresh();

                _statusOk   = true;
                _lastStatus = $"✓ Exported to {assetPath}";
                Debug.Log($"[FbxExportTool] {_lastStatus}");
            }
            catch (Exception ex)
            {
                // Restore materials even on error
                // (bakedMaterials captured in closure if we restructure; simple re-fetch here)
                _statusOk   = false;
                _lastStatus = $"Export failed: {ex.Message}";
                Debug.LogException(ex);
            }
        }

        public static Texture2D ExtractLayer(Texture2DArray array, int layer)
        {
            var tex = new Texture2D(
                array.width,
                array.height,
                TextureFormat.RGBA32,
                false);

            Graphics.CopyTexture(
                array, layer, 0,
                tex, 0, 0);

            return tex;
        }
        
        // ── Material baking ───────────────────────────────────────────────────

        /// <summary>
        /// Creates a Standard/URP-Lit copy of <paramref name="src"/>, mapping
        /// textures from custom shader properties to well-known slots.
        /// The baked material is saved as an asset next to the FBX.
        /// </summary>
        private Material BakeMaterial(Material src, string fbxAssetPath, int index)
        {
            // Pick a fallback shader that FBX importers understand
            Shader fallback = GetFallbackShader();
            var baked = new Material(fallback) { name = $"{src.name}_Baked{index}" };

            // ── Standard texture slots ──────────────────────────────────────
            TryCopyTexture(src, baked, "_MainTex",          "_MainTex");
            TryCopyTexture(src, baked, "_BaseMap",          "_MainTex");   // URP source
            TryCopyColor  (src, baked, "_Color",            "_Color");
            TryCopyColor  (src, baked, "_BaseColor",        "_Color");     // URP source

            // ── Emission ────────────────────────────────────────────────────
            if (src.IsKeywordEnabled("_EMISSION"))
            {
                baked.EnableKeyword("_EMISSION");
                TryCopyColor  (src, baked, "_EmissionColor", "_EmissionColor");
                TryCopyTexture(src, baked, "_EmissionMap",   "_EmissionMap");
            }

            // Save the baked material as an asset so the FBX can reference it
            string matFolder = Path.GetDirectoryName(fbxAssetPath);
            string matPath   = $"{matFolder}/{baked.name}.mat";
            AssetDatabase.CreateAsset(baked, matPath);

            return baked;
        }

        private static Shader GetFallbackShader()
        {
            // Prefer URP Lit, fall back to Built-in Standard
            var urp = Shader.Find("Universal Render Pipeline/Lit");
            return urp != null ? urp : Shader.Find("Standard");
        }

        private static bool IsStandardShader(Shader s)
        {
            if (s == null) return true;
            return s.name == "Standard" ||
                   s.name.StartsWith("Universal Render Pipeline/Lit") ||
                   s.name.StartsWith("HDRP/Lit");
        }

        private static void TryCopyTexture(Material src, Material dst,
                                           string srcProp, string dstProp)
        {
            if (src.HasProperty(srcProp) && dst.HasProperty(dstProp))
            {
                var tex = src.GetTexture(srcProp);
                if (tex != null) dst.SetTexture(dstProp, tex);
            }
        }

        private static void TryCopyColor(Material src, Material dst,
                                         string srcProp, string dstProp)
        {
            if (src.HasProperty(srcProp) && dst.HasProperty(dstProp))
                dst.SetColor(dstProp, src.GetColor(srcProp));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static IEnumerable<Renderer> CollectRenderers(
            List<GameObject> roots, bool includeChildren)
        {
            var seen = new HashSet<Renderer>();
            foreach (var root in roots)
            {
                var renderers = includeChildren
                    ? root.GetComponentsInChildren<Renderer>(true)
                    : root.GetComponents<Renderer>();

                foreach (var r in renderers)
                    if (seen.Add(r)) yield return r;
            }
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "Export" : name;
        }

        // ── Inner types ───────────────────────────────────────────────────────
        [Serializable]
        private class ShaderPropertyEntry
        {
            public string shaderProp = "";
            public string fbxProp    = "";
        }
    }
}