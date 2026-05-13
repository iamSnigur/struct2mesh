using Core.AssetManagement;
using Core.AssetManagement.External;
using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(this);
        //MinecraftAssetProvider.WarmupAsync().Forget();
        MinecraftAssetProvider.Init().Forget();
    }

    private void OnDestroy()
    {
        AssetProvider.Release();
    }
}