using System;
using Core.AssetManagement;
using Cysharp.Threading.Tasks;
using Logic;
using Logic.PlayerPreview;
using UI;
using UI.Views;
using UnityEngine;
using UnityEngine.Assertions;

namespace Core
{
    public class Bootstrapper : MonoBehaviour
    {
        private CameraController _cameraController;
        private StructureBuilder _structureBuilder;
        private FileReceiver _fileReceiver;
        private LoadingCurtainView _loadingCurtainView;
        private PlayerPuppet _playerPuppet;
        private UIRoot _uiRoot;

        private async void Awake()
        {
            try
            {
                BindDependencies();
                SkinLoader skinLoader = new();
                await skinLoader.Init();
                _loadingCurtainView.SetProgressText("Loading boot assets...");
                _loadingCurtainView.Show();
                await MinecraftAssetProvider.Init();
                await UniTask.Delay(500);
                _loadingCurtainView.Hide();
            }
            catch (Exception e)
            {
                _loadingCurtainView.SetProgressText("Failed to load assets");
                Debug.LogError(e);
            }
        }

        private void OnDestroy()
        {
            MinecraftAssetProvider.Release();
        }

        private void BindDependencies()
        {
            _cameraController = GetOnScene<CameraController>();
            Assert.IsNotNull(_cameraController);
            
            _structureBuilder = GetOnScene<StructureBuilder>();
            Assert.IsNotNull(_structureBuilder);
            
            _fileReceiver = GetOnScene<FileReceiver>();
            Assert.IsNotNull(_fileReceiver);

            _playerPuppet = GetOnScene<PlayerPuppet>();
            Assert.IsNotNull(_playerPuppet);
            
            _uiRoot = GetOnScene<UIRoot>();
            Assert.IsNotNull(_uiRoot);
            
            _uiRoot.InjectDependencies(
                _cameraController,
                _fileReceiver,
                _structureBuilder,
                _playerPuppet);
            
            _loadingCurtainView = _uiRoot.LoadingCurtainView;
            _fileReceiver.InjectDependencies(_structureBuilder);
            _structureBuilder.InjectDependencies(_loadingCurtainView, _cameraController);
        }

        private static T GetOnScene<T>() where T : MonoBehaviour => 
            FindFirstObjectByType<T>();
    }
}