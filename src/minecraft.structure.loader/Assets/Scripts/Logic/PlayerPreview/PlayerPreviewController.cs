using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;

namespace Logic.PlayerPreview
{
    public class PlayerPreviewController : MonoBehaviour
    {
        private const string PLAYER_NAME = "SNIGUR";
        
        public Camera PuppetCamera;
        [FormerlySerializedAs("PlayerPuppet")] public PlayerPuppet Puppet;
        private readonly SkinLoader _skinLoader = new();

        public RenderTexture RenderTexture { get; private set; }

        public void Init()
        {
            RenderTexture = new RenderTexture(
                1024,
                1024,
                GraphicsFormat.R16G16B16A16_UNorm,
                GraphicsFormat.D16_UNorm, 
                0)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false
            };
            RenderTexture.Create();
            PuppetCamera.targetTexture = RenderTexture;
            
            Puppet.Init(PuppetCamera);
            _skinLoader.Init();
            _skinLoader.LoadPlayerSkinAsync(PLAYER_NAME);

            ToggleCamera(false);
            Hide();
        }

        public void Show()
        {
            Puppet.Show();
        }

        public void Hide()
        {
            Puppet.Hide();
        }

        public void ToggleCamera(bool active)
        {
            PuppetCamera.gameObject.SetActive(active);
        }
    }
}