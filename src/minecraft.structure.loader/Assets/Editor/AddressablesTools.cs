using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Editor
{
    public static class AddressablesTools
    {
        [MenuItem("Tools/Addressables/Update Texture Meta Files Addresses")]
        public static void UpdateTextureMetaFilesAddress()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressables settings not found.");
                return;
            }

            int count = 0;

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null) continue;
                
                if (!group.name.Equals("Textures")) continue;

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    string address = entry.address;

                    if (address.EndsWith(".png"))
                        address = address.Replace(".png", ".mcmeta");
                    
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
        
        [MenuItem("Tools/Addressables/Clean Asset Addresses")]
        public static void CleanAddresses()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressables settings not found.");
                return;
            }

            int count = 0;

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null) continue;

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    string address = entry.address;

                    if (address.StartsWith("Assets/Resources_moved/"))
                        address = address["Assets/Resources_moved/".Length..];

                    if (address.EndsWith(".png"))
                        address = address[..^".png".Length];
                    
                    if (address.EndsWith(".json"))
                        address = address[..^".json".Length];

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