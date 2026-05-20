using Core.AssetManagement;
using Core.AssetManagement.External;
using UnityEngine;

namespace Core
{
    public class Bootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(this);
            
            BuildInfo.Init();
            MinecraftAssetProvider.Init().Forget();
        }

        private void OnDestroy()
        {
            AssetProvider.Release();
        }
    }
}