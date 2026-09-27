using System;
using System.Threading;
using Core.AssetManagement;
using Cysharp.Threading.Tasks;
using Integrations.Mojang;
using Logic.PlayerPreview;
using UI.Views;
using UnityEngine;
using UnityEngine.Rendering;
using VContainer;
using VContainer.Unity;

namespace Core.Bootstrap
{
    public class Bootstrapper : IAsyncStartable, IDisposable
    {
        private readonly ILoadingCurtain _loadingCurtain;
        private readonly IPlayerPreview _playerPreview;
        private readonly SkinLoader _skinLoader;
        private readonly MainView _mainView;
        
        [Inject]
        public Bootstrapper(
            ILoadingCurtain loadingCurtain,
            IPlayerPreview playerPreview,
            MainView mainView,
            SkinLoader skinLoader)
        {
            _loadingCurtain = loadingCurtain;
            _playerPreview = playerPreview;
            _mainView = mainView;
            _skinLoader = skinLoader;
        }

        public async UniTask StartAsync(CancellationToken ct)
        {
            try
            {
                _loadingCurtain.SetInfoText("Loading boot assets...");
                _loadingCurtain.Show();
                
                await MinecraftAssetProvider.Init();
                
                await UniTask.Delay(500, cancellationToken: ct);
                _loadingCurtain.Hide();
                
                _mainView.Show();
                
                LoadPuppetSkinAsync(ct).Forget();
            }
            catch (Exception e)
            {
                _loadingCurtain.SetInfoText("Failed to load assets");
                Debug.LogError(e);
            }
        }

        public void Dispose()
        {
            MinecraftAssetProvider.Release();
        }
        
        private async UniTaskVoid LoadPuppetSkinAsync(CancellationToken ct)
        {
            PlayerSkinData skinData = await _skinLoader.LoadPlayerSkinAsync("SNIGUR", ct);
            _playerPreview.ApplySkin(skinData);
        }
    }
}