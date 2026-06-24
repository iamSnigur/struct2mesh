using Core.AssetManagement;
using Core.AssetManagement.External;
using Core.UI;
using UnityEngine;

namespace Core
{
    public class Bootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(this);
            MinecraftAssetProvider.Init().Forget();
            BindDependencies();
        }

        private void OnDestroy()
        {
            AssetProvider.Release();
        }

        private static void BindDependencies()
        {
            CameraController cameraController =         GetOnScene<CameraController>();
            NbtStructureParser nbtStructureParser =     GetOnScene<NbtStructureParser>();
            FileReceiver fileReceiver =                 GetOnScene<FileReceiver>();
            LoadingCurtain loadingCurtain =             GetOnScene<LoadingCurtain>();
            RootUI rootUI =                             GetOnScene<RootUI>();
            
            nbtStructureParser.InjectDependencies(loadingCurtain, cameraController);
            fileReceiver.InjectDependencies(nbtStructureParser);
            rootUI.InjectDependencies(cameraController, fileReceiver, nbtStructureParser);
        }

        private static T GetOnScene<T>() where T : MonoBehaviour => 
            FindFirstObjectByType<T>();
    }
}