using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Logic.PlayerPreview
{
    public class PlayerPreviewController : MonoBehaviour
    {
        public Camera PuppetCamera;
        public PlayerPuppet PlayerPuppet;
        //public SkinLoader SkinLoader;

        public RenderTexture PlayerPreviewTexture { get; private set; }

        public void Init()
        {
            PlayerPreviewTexture = new RenderTexture(
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
            PlayerPreviewTexture.Create();
            PuppetCamera.targetTexture = PlayerPreviewTexture;
            
            PlayerPuppet.Init(PuppetCamera);
            // load skins
            
            // hide camera
            // hide puppet
        }
    }
}