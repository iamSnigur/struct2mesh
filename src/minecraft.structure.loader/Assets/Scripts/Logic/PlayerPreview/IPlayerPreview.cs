using Integrations.Mojang;
using UnityEngine;

namespace Logic.PlayerPreview
{
    public interface IPlayerPreview
    {
        RenderTexture RenderTexture { get; }
        void SetPreviewActive(bool active);
        void ApplySkin(PlayerSkinData skinData);
        void Show();
        void Hide();
        void LookAt(Vector2 screenPos);
        void Punch();
    }
}