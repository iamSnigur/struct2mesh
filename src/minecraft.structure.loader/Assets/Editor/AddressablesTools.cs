using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Editor
{
    public static class AddressablesTools
    {
        [MenuItem("Tools/Addressables/Label Texture Source Group")]
        public static void LabelGroup()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            string groupName = "In-Editor Assets";
            string label = "texture-source";

            settings.AddLabel(label);

            AddressableAssetGroup group = settings.FindGroup(groupName);

            if (group == null)
            {
                Debug.LogError("Group not found"); 
                return;
            }

            foreach (AddressableAssetEntry entry in group.entries)
            {
                entry.SetLabel(label, true, true);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Labeled {group.entries.Count} entries in '{groupName}' with '{label}'");
        }
        
        [MenuItem("Tools/Addressables/Clean Asset Addresses")]
        public static void CleanAddresses()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressables settings not found.");
                return;
            }

            int count = 0;

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null) continue;

                foreach (var entry in group.entries)
                {
                    string address = entry.address;

                    if (address.StartsWith("Assets/Resources_moved/"))
                        address = address["Assets/Resources_moved/".Length..];

                    if (address.EndsWith(".png"))
                        address = address[..^".png".Length];

                    if (address != entry.address)
                    {
                        entry.SetAddress(address);
                        count++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Cleaned {count} Addressable addresses.");
        }
    }
}