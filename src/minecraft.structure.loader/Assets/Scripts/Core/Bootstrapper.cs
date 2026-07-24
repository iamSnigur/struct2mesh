using System;
using Core.AssetManagement;
using Core.AssetManagement.External;
using Cysharp.Threading.Tasks;
using Logic;
using UI;
using UI.Views;
using UnityEngine;

namespace Core
{
    public class Bootstrapper : MonoBehaviour
    {
        private CameraController _cameraController;
        private StructureBuilder _structureBuilder;
        private FileReceiver _fileReceiver;
        private LoadingCurtainView _loadingCurtainView;
        private UIRoot _uiRoot;

        private async void Awake()
        {
            try
            {
                DontDestroyOnLoad(this);
                await BindDependencies();
                _loadingCurtainView.SetProgressText("Loading boot assets...");
                _loadingCurtainView.Show();
                await MinecraftAssetProvider.Init();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                await UniTask.Delay(500);
                _loadingCurtainView.Hide();
            }
        }

        private void OnDestroy()
        {
            AssetProvider.Release();
        }

        private async UniTask BindDependencies()
        {
            _cameraController = GetOnScene<CameraController>();
            _structureBuilder = GetOnScene<StructureBuilder>();
            _fileReceiver = GetOnScene<FileReceiver>();
            _uiRoot = GetOnScene<UIRoot>();
            
            OnboardingSlidesConfig slidesConfig = 
                await AssetProvider
                    .LoadByAddressAsync<OnboardingSlidesConfig>("onboarding_slides_config");
            
            _uiRoot.InjectDependencies(
                _cameraController,
                _fileReceiver,
                _structureBuilder,
                slidesConfig);
            
            _loadingCurtainView = _uiRoot.LoadingCurtainView;
            _fileReceiver.InjectDependencies(_structureBuilder);
            _structureBuilder.InjectDependencies(_loadingCurtainView, _cameraController);
        }

        private static T GetOnScene<T>() where T : MonoBehaviour => 
            FindFirstObjectByType<T>();
    }
}