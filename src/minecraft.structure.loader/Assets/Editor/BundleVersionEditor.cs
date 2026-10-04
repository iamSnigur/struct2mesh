using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class BundleVersionEditor : EditorWindow
    {
        private string _version;
        
        [MenuItem("Tools/Build/Bundle Version")]
        public static void ShowWindow() => 
            GetWindow<BundleVersionEditor>("Bundle Version");

        private void OnEnable()
        {
            _version = PlayerSettings.bundleVersion;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            _version = EditorGUILayout.TextField("Version", _version);
            
            EditorGUILayout.Space();
            
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Reload"))
            {
                _version = PlayerSettings.bundleVersion;
            }

            if (GUILayout.Button("Save"))
            {
                PlayerSettings.bundleVersion = _version;
            }
            
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
    }
}