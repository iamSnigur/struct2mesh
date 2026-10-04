using System.Collections;
using Integrations.Mojang;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Logic.PlayerPreview
{
    public class PlayerPreviewController : MonoBehaviour, IPlayerPreview
    {
        private const int TEXTURE_SIZE = 512;
        
        public Camera PuppetCamera;
        public PlayerPuppet Puppet;
        
        public RenderTexture RenderTexture { get; private set; }

        private void Awake()
        {
            RenderTexture = new RenderTexture(
                TEXTURE_SIZE,
                TEXTURE_SIZE,
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

            Hide();
            ApplySkin(null);
        }

        // Puts puppet's rigidbodies into sleep after physics initialized
        private IEnumerator Start()
        {
            yield return new WaitForFixedUpdate();
            SetPreviewActive(false);
        }

        private void OnDestroy()
        {
            RenderTexture.Release();
            Destroy(RenderTexture);
        }

        public void ApplySkin(PlayerSkinData skinData) => Puppet.ApplySkin(skinData);

        public void Show() => Puppet.Show();

        public void Hide() => Puppet.Hide();

        public void Punch() => Puppet.Punch();

        public void SetPreviewActive(bool active)
        {
            PuppetCamera.gameObject.SetActive(active);
            
            if (active)
                Puppet.WakeUp();
            else
                Puppet.Sleep();
        }

        public void LookAt(Vector2 screenPos)
        {
            Vector3 target = ScreenToWorld(screenPos);
            Puppet.LookAt(target);
        }

        private Vector3 ScreenToWorld(Vector2 screenPos)
        {
            Ray worldRay = PuppetCamera.ViewportPointToRay(screenPos);
            Vector3 target = worldRay.GetPoint(1f);
            target.x *= -1f;
            return target;
        }
    }
}