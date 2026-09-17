using System;
using Cysharp.Threading.Tasks;
using Integrations.Mojang.Internal;
using Integrations.Utils;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Integrations.Mojang
{
    public class MojangService
    {
        private const string GET_PROFILE_ENDPOINT = "https://api.mojang.com/users/profiles/minecraft";

        private const string GET_SKIN_PROPERTIES_ENDPOINT =
            "https://sessionserver.mojang.com/session/minecraft/profile";

        public async UniTask<PlayerSkinData> GetPlayerSkinDataAsync(string playerName)
        {
            ProfileData profileData = await GetProfileAsync(playerName);
            SkinProperties skinProperties = await GetSkinPropertiesAsync(profileData);

            string propertiesDecoded = Base64Utils.Decode(skinProperties.properties[0].value);
            PropertiesData propertiesData = JsonConvert.DeserializeObject<PropertiesData>(propertiesDecoded);
            
            return await GetPlayerSkinAsync(propertiesData);
        }

        private async UniTask<PlayerSkinData> GetPlayerSkinAsync(PropertiesData propertiesData)
        {
            Texture2D skinTexture = await GetTextureAsync(ToHttps(propertiesData.textures.SKIN.url));
            Texture2D capeTexture = await GetTextureAsync(ToHttps(propertiesData.textures.CAPE.url));
            
            return new PlayerSkinData
            {
                SkinTexture = skinTexture,
                CapeTexture = capeTexture
            };
        }

        private async UniTask<ProfileData> GetProfileAsync(string name)
        {
            string uri = $"{GET_PROFILE_ENDPOINT}/{name}";
            string response = await GetStringAsync(uri);
            ProfileData profileData = JsonConvert.DeserializeObject<ProfileData>(response);
            return profileData;
        }

        private async UniTask<SkinProperties> GetSkinPropertiesAsync(ProfileData profileData)
        {
            string uri = $"{GET_SKIN_PROPERTIES_ENDPOINT}/{profileData.id}";
            string response = await GetStringAsync(uri);
            SkinProperties skinProperties = JsonConvert.DeserializeObject<SkinProperties>(response);
            return skinProperties;
        }

        private static string ToHttps(string url)
        {
            if (url != null
                && url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                return "https://" + url.Substring("http://".Length);

            return url;
        }

        private static async UniTask<Texture2D> GetTextureAsync(string uri, int timeout = 10)
        {
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