using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Core.UI
{
    public class WelcomeHubPopup : MonoBehaviour
    {
        public OnboardingSlidesConfig SlidesConfig;
        
        public AnimationCurve ShowAnimationCurve;
        public AnimationCurve HideAnimationCurve;

        public SpriteAnimator SpriteAnimator;

        public Color OverlayShownColor;
        public Color OverlayHiddenColor;
        
        private VisualElement _popupOverlay;
        private VisualElement _popupRoot;

        private VisualElement _welcomeHubRoot;
        private VisualElement _onboardingRoot;

        private Label _slideTitle;
        private VisualElement _slideImage;
        private Label _slideDescription;
        
        private Label _slidesProgressLabel;
        private Button _leftArrow;
        private Button _rightArrow;

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

            _slideTitle = _popupRoot.Q<Label>("onboarding-title");
            _slideDescription = _popupRoot.Q<Label>("onboarding-description");
            _slideImage = _popupRoot.Q<VisualElement>("onboarding-image");
            
            _slidesProgressLabel = _popupRoot.Q<Label>("slide-progress-label");
            
            _leftArrow = _popupRoot.Q<Button>("btn-arrow-left");
            _leftArrow.clicked += LeftArrowClicked;
            
            _rightArrow = _popupRoot.Q<Button>("btn-arrow-right");
            _rightArrow.clicked += RightArrowClicked;
        }

        private int _currentSlide;
        
        private void LeftArrowClicked()
        {
            int totalSlides = SlidesConfig.Slides.Length;
            _currentSlide = Mathf.Clamp(_currentSlide - 1, 0, totalSlides);

            _leftArrow.SetEnabled(_currentSlide != 0);
            _rightArrow.SetEnabled(_currentSlide != totalSlides - 1);
            SetSlideByIndex(_currentSlide);
        }

        private void RightArrowClicked()
        {
            int totalSlides = SlidesConfig.Slides.Length;
            _currentSlide = Mathf.Clamp(_currentSlide + 1, 0, totalSlides);

            _leftArrow.SetEnabled(_currentSlide != 0);
            _rightArrow.SetEnabled(_currentSlide != totalSlides - 1);
            SetSlideByIndex(_currentSlide);
        }
        
        private void SetSlideByIndex(int index)
        {
            if (index < 0 || index >= SlidesConfig.Slides.Length)
                return;
            
            OnboardingSlide slide = SlidesConfig.Slides[index];
            _slideTitle.text = slide.Title;
            _slideDescription.text = slide.Description;
            _slideImage.style.backgroundImage = new StyleBackground(slide.Image);

            _slidesProgressLabel.text = $"{index + 1}/{SlidesConfig.Slides.Length}";
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
            
            _currentSlide = 0;
            LeftArrowClicked();
            
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