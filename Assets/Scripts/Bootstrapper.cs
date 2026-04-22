using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(this);
        AssetProvider.Init().Forget();
    }
}