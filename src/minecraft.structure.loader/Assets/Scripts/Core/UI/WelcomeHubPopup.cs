using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Core.UI
{
    public class WelcomeHubPopup : MonoBehaviour
    {
        public AnimationCurve ShowAnimationCurve;
        public AnimationCurve HideAnimationCurve;

        public SpriteAnimator SpriteAnimator;
        
        private VisualElement _popupOverlay;
        private VisualElement _popupRoot;

        private float _popupHideHeight;
        
        private void Awake()
        {
            UIDocument doc = GetComponent<UIDocument>();
            _popupOverlay = doc.rootVisualElement.Q<VisualElement>("popup-overlay");
            _popupRoot = _popupOverlay.Q<VisualElement>("popup-root");
            
            _popupOverlay.style.display = DisplayStyle.Flex;
            _popupRoot.RegisterCallbackOnce<GeometryChangedEvent>(_ =>
            {
                _popupHideHeight = -_popupRoot.resolvedStyle.height - 200f;
                Debug.Log(_popupHideHeight);
                _popupOverlay.style.display = DisplayStyle.None;
            });

            _popupOverlay.RegisterCallback<PointerDownEvent>(_ => Hide());
            _popupRoot.RegisterCallback<PointerDownEvent>(ev => ev.StopPropagation());
        }

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                Show();
        }

        public void Show()
        {
            _popupOverlay.style.display = DisplayStyle.Flex;
            SpriteAnimator.StartAnimation();
            Animate(ShowAnimationCurve, _popupHideHeight, 0f, 0.5f).Forget();
        }

        public void Hide()
        {
            Animate(HideAnimationCurve, 0, _popupHideHeight, 0.5f, 
                onComplete: () =>
                {
                    _popupOverlay.style.display = DisplayStyle.None;
                    SpriteAnimator.PauseAnimation();
                }).Forget();
        }
        
        private async UniTaskVoid Animate(
            AnimationCurve curve,
            float from, float to,
            float duration = 1f,
            Action onComplete = null)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float normalized = elapsed / duration;
                float t = curve.Evaluate(normalized);
                //_popupOverlay.style.color = new StyleColor(new Color(0,0,0, 0));
                SetTopOffset(Mathf.LerpUnclamped(from, to, t));
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }

            SetTopOffset(to);
            onComplete?.Invoke();
        }
        
        private void SetTopOffset(float top)
        {
            _popupRoot.style.top = top;
        }
    }
}