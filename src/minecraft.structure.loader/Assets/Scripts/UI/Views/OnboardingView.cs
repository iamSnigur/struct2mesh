using UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class OnboardingView : ViewComponent
    {
        private readonly OnboardingSlidesConfig _slidesConfig;
        
        private Label _slideTitle;
        private VisualElement _slideImage;
        private Label _slideDescription;
        
        private Label _slidesProgressLabel;
        private Button _leftArrow;
        private Button _rightArrow;
        
        private int _currentSlide;
        
        public OnboardingView(VisualElement root, OnboardingSlidesConfig slidesConfig) : base(root)
        {
            _slidesConfig = slidesConfig;
        }

        protected override void QueryElements()
        {
            _slideTitle = Query<Label>("onboarding-title");
            _slideDescription = Query<Label>("onboarding-description");
            _slideImage = Query<VisualElement>("onboarding-image");
            _slidesProgressLabel = Query<Label>("slide-progress-label");
            
            _leftArrow = Query<Button>("btn-arrow-left");
            _rightArrow = Query<Button>("btn-arrow-right");
        }

        protected override void RegisterCallbacks()
        {
            _leftArrow.clicked += LeftArrowClicked;
            _rightArrow.clicked += RightArrowClicked;
        }

        public override void Show() => 
            _root.style.display = DisplayStyle.Flex;

        public override void Hide() => 
            _root.style.display = DisplayStyle.None;
        
        private void LeftArrowClicked()
        {
            int totalSlides = _slidesConfig.Slides.Length;
            _currentSlide = Mathf.Clamp(_currentSlide - 1, 0, totalSlides);

            _leftArrow.SetEnabled(_currentSlide != 0);
            _rightArrow.SetEnabled(_currentSlide != totalSlides - 1);
            SetSlideByIndex(_currentSlide);
        }

        private void RightArrowClicked()
        {
            int totalSlides = _slidesConfig.Slides.Length;
            _currentSlide = Mathf.Clamp(_currentSlide + 1, 0, totalSlides);

            _leftArrow.SetEnabled(_currentSlide != 0);
            _rightArrow.SetEnabled(_currentSlide != totalSlides - 1);
            SetSlideByIndex(_currentSlide);
        }
        
        private void SetSlideByIndex(int index)
        {
            if (index < 0 || index >= _slidesConfig.Slides.Length)
                return;
            
            OnboardingSlide slide = _slidesConfig.Slides[index];
            _slideTitle.text = slide.Title;
            _slideDescription.text = slide.Description;
            _slideImage.style.backgroundImage = new StyleBackground(slide.Image);

            _slidesProgressLabel.text = $"{index + 1}/{_slidesConfig.Slides.Length}";
        }
    }
}