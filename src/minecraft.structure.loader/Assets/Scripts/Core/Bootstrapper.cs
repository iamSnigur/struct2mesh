using System;
using Core.AssetManagement;
using Core.AssetManagement.External;
using Cysharp.Threading.Tasks;
using Logic;
using UI;
using UnityEngine;

namespace Core
{
    public class Bootstrapper : MonoBehaviour
    {
        private CameraController _cameraController;
        private StructureBuilder _structureBuilder;
        private FileReceiver _fileReceiver;
        private LoadingCurtain _loadingCurtain;
        private RootUI _rootUI;

        private async void Awake()
        {
            DontDestroyOnLoad(this);
            GetSceneReferences();
            InjectDependencies();
            
            try
            {
                _loadingCurtain.SetProgressText("Loading assets...");
                _loadingCurtain.Show();
                await MinecraftAssetProvider.Init();
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
            finally
            {
                await UniTask.Delay(500);
                _loadingCurtain.Hide();
            }
        }

        private void OnDestroy() => 
            AssetProvider.Release();

        private void InjectDependencies()
        {
            _structureBuilder.InjectDependencies(_loadingCurtain, _cameraController);
            _fileReceiver.InjectDependencies(_structureBuilder);
            _rootUI.InjectDependencies(_cameraController, _fileReceiver, _structureBuilder);
        }

        private void GetSceneReferences()
        {
            _cameraController = GetOnScene<CameraController>();
            _structureBuilder = GetOnScene<StructureBuilder>();
            _fileReceiver = GetOnScene<FileReceiver>();
            _loadingCurtain = GetOnScene<LoadingCurtain>();
            _rootUI = GetOnScene<RootUI>();
        }

        private static T GetOnScene<T>() where T : MonoBehaviour => 
            FindFirstObjectByType<T>();
    }
}