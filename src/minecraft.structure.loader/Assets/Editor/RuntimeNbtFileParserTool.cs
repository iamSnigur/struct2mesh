using System.IO;
using Core;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class RuntimeNbtFileParserTool
    {
        private static StructureBuilder _builder;
        
        [MenuItem("Assets/Load NBT Structure", true)]
        private static bool ValidateProcessFile()
        {
            if (!Application.isPlaying)
                return false;
            
            if (Selection.activeObject == null)
                return false;

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);

            return Path.GetExtension(path).Equals(".nbt", System.StringComparison.OrdinalIgnoreCase);
        }

        [MenuItem("Assets/Load NBT Structure")]
        private static void ProcessFile()
        {
            _builder ??= Object.FindFirstObjectByType<StructureBuilder>();
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            Debug.Log($"Processing {path}");
            var fileContent = File.ReadAllBytes(path);
            _builder.BuildFromBytesAsync(fileContent, "nbt").Forget();
        }
    }
}