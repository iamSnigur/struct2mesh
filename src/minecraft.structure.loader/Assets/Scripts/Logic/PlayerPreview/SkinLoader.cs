using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Integrations.Mojang;
using VContainer;

namespace Logic.PlayerPreview
{
    public class SkinLoader // SkinService
    {
        private PlayerSkinData _playerSkinData;

        private readonly MojangService _mojangService;

        [Inject]
        public SkinLoader(MojangService mojangService) => 
            _mojangService = mojangService;

        public async UniTask<PlayerSkinData> LoadPlayerSkinAsync(string playerName, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(playerName))
                return null;
            
            try
            {
                _playerSkinData = await _mojangService.GetPlayerSkinDataAsync(playerName, ct);
                return _playerSkinData;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }
    }
}