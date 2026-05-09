using Core.AssetManagement;
using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(this);
        MinecraftAssetProvider.WarmupAsync().Forget();
        //MinecraftAssetProvider.Init().Forget();
    }
}