using System;
using Cysharp.Threading.Tasks;
using Integrations.Mojang.Internal;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Integrations.Mojang
{
    public class MojangService
    {
        // Mojang api does not send "Access-Control-Allow-Origin" header
        // So integration was done with third-party api
        private const string GET_PLAYER_DATA_ENDPOINT = "https://playerdb.co/api/player/minecraft";

        public async UniTask<PlayerSkinData> GetPlayerSkinDataAsync(string playerName)
        {
            PlayerDbResponse db = await GetPlayerDbResponseAsync(playerName);

            if (!db.Success)
                throw new Exception();

            return await GetPlayerSkinAsync(db);
        }

        private async UniTask<PlayerDbResponse> GetPlayerDbResponseAsync(string name)
        {
            string uri = $"{GET_PLAYER_DATA_ENDPOINT}/{name}";
            string response = await GetStringAsync(uri);
            PlayerDbResponse playerDbResponse = JsonConvert.DeserializeObject<PlayerDbResponse>(response);
            return playerDbResponse;
        }

        private async UniTask<PlayerSkinData> GetPlayerSkinAsync(PlayerDbResponse playerDb)
        {
            Player player = playerDb.Data.Player;
            Texture2D skinTexture = await GetTextureAsync(player.SkinTexture);
            Texture2D capeTexture = await GetTextureAsync(player.CapeTexture);
            
            return new PlayerSkinData
            {
                SkinTexture = skinTexture,
                CapeTexture = capeTexture
            };
        }

        private static async UniTask<Texture2D> GetTextureAsync(string uri, int timeout = 10)
        {
            if (string.IsNullOrEmpty(uri))
                return null;
            
            byte[] textureBytes = await GetBytesAsync(uri, timeout);
            Texture2D texture = new(1, 1, TextureFormat.RGBA32, false);
            texture.LoadImage(textureBytes);
            texture.filterMode = FilterMode.Point;
            texture.Apply();

            return texture;
        }

        private static async UniTask<byte[]> GetBytesAsync(string uri, int timeout = 10)
        {
            UnityWebRequest request = UnityWebRequest.Get(uri);
            request.timeout = timeout;
            await request.SendWebRequest();

            return request.downloadHandler.data;
        }

        private static async UniTask<string> GetStringAsync(string uri, int timeout = 10)
        {
            UnityWebRequest request = UnityWebRequest.Get(uri);
            request.timeout = timeout;
            await request.SendWebRequest();

            return request.downloadHandler.text;
        }
    }
}