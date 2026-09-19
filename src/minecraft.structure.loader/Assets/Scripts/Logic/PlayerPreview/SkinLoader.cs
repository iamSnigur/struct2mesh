using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Integrations.Mojang;

namespace Logic.PlayerPreview
{
    public class SkinLoader
    {
        private Texture2D _defaultSkin;
        private Texture2D _defaultCape;
        private Material _skinMaterial;
        private Material _capeMaterial;
        private PlayerSkinData _playerSkinData;

        private readonly MojangService _mojangService = new();

        public void Init()
        {
            _defaultSkin = Resources.Load<Texture2D>("Player/default_skin");
            _defaultCape = Resources.Load<Texture2D>("Player/default_cape");
            _skinMaterial = Resources.Load<Material>("Player/SkinMaterial");
            _capeMaterial = Resources.Load<Material>("Player/CapeMaterial");
        }

        public async UniTask LoadPlayerSkinAsync(string playerName)
        {
            try
            {
                _playerSkinData = await _mojangService.GetPlayerSkinDataAsync(playerName);
                SetSkinTextures(_playerSkinData.SkinTexture, _playerSkinData.CapeTexture);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                SetSkinTextures(_defaultSkin, _defaultCape);
            }
        }
        
        private void SetSkinTextures(Texture2D skin, Texture2D cape)
        {
            _skinMaterial.mainTexture = skin;
            _capeMaterial.mainTexture = cape;
        }
    }
}