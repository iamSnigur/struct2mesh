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

        public Color OverlayShownColor;
        public Color OverlayHiddenColor;
        
        private VisualElement _popupOverlay;
        private VisualElement _popupRoot;

        private VisualElement _welcomeHubRoot;
        private VisualElement _onboardingRoot;

        private float _popupHideHeight;
        private bool _isAnimating;
        
        private void Awake()
        {
            UIDocument doc = GetComponent<UIDocument>();
            _popupOverlay = doc.rootVisualElement.Q<VisualElement>("popup-overlay");
            _popupRoot = _popupOverlay.Q<VisualElement>("popup-root");

            _welcomeHubRoot = _popupOverlay.Q<VisualElement>("welcome-hub-root");
            _onboardingRoot = _popupOverlay.Q<VisualElement>("onboarding-root");
            
            _popupOverlay.style.display = DisplayStyle.Flex;
            _popupRoot.RegisterCallbackOnce<GeometryChangedEvent>(_ =>
            {
                _popupHideHeight = -_popupRoot.resolvedStyle.height - 200f;
                Debug.Log(_popupHideHeight);
                _popupOverlay.style.display = DisplayStyle.None;
            });

            _popupOverlay.RegisterCallback<PointerDownEvent>(
                ev =>
                {
                    if (ev.target == _popupOverlay)
                        Hide();
                }, TrickleDown.TrickleDown);

            Button start = _popupOverlay.Q<Button>("btn-start-onboarding");
            start.clicked += () =>
            {
                _onboardingRoot.style.display = DisplayStyle.Flex;
                _welcomeHubRoot.style.display = DisplayStyle.None;
            };
            
            Button back = _popupOverlay.Q<Button>("btn-back");
            back.clicked += () =>
            {
                _onboardingRoot.style.display = DisplayStyle.None;
                _welcomeHubRoot.style.display = DisplayStyle.Flex;
            };

            Button left = _popupRoot.Q<Button>("btn-arrow-left");
            left.clicked += () => left.SetEnabled(false);
        }

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                Show();
        }

        public void Show()
        {
            if (_isAnimating)
                return;
            
            _popupOverlay.style.display = DisplayStyle.Flex;
            SpriteAnimator.StartAnimation();
            Animate(
                ShowAnimationCurve, 
                _popupHideHeight, 0f,
                OverlayHiddenColor, OverlayShownColor,
                0.5f).Forget();
        }

        public void Hide()
        {
            if (_isAnimating)
                return;
            
            Animate(HideAnimationCurve, 
                0, _popupHideHeight,
                OverlayShownColor, OverlayHiddenColor,
                0.5f, 
                onComplete: () =>
                {
                    _popupOverlay.style.display = DisplayStyle.None;
                    SpriteAnimator.PauseAnimation();
                }).Forget();
        }
        
        private async UniTaskVoid Animate(
            AnimationCurve curve,
            float from, float to,
            Color from1, Color to1,
            float duration = 1f,
            Action onComplete = null)
        {
            float elapsed = 0f;

            _isAnimating = true;
            while (elapsed < duration)
            {
                float normalized = elapsed / duration;
                float t = curve.Evaluate(normalized);
                _popupOverlay.style.backgroundColor = Color.Lerp(from1, to1, t);// new StyleColor(new Color(0,0,0, 0));
                SetTopOffset(Mathf.LerpUnclamped(from, to, t));
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }

            SetTopOffset(to);
            onComplete?.Invoke();
            _isAnimating = false;
        }
        
        private void SetTopOffset(float top)
        {
            _popupRoot.style.top = top;
        }
    }
}