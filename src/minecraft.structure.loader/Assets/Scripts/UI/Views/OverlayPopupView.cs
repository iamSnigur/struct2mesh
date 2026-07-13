using UI.Components;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class OverlayPopupView : ViewComponent
    {
        private readonly WelcomeHubView _welcomeHubView;
        private readonly OnboardingView _onboardingView;

        private Button _startOnboardingButton;
        private Button _goBackButton;
        
        public OverlayPopupView(VisualElement root, OnboardingSlidesConfig slidesConfig) : base(root)
        {
            _welcomeHubView = new WelcomeHubView(Query<VisualElement>("welcome-hub-root"));
            _onboardingView = new OnboardingView(
                Query<VisualElement>("onboarding-root"), 
                slidesConfig);
            
            _root.pickingMode = PickingMode.Ignore;
        }

        protected override void QueryElements()
        {
            _startOnboardingButton = Query<Button>("btn-start-onboarding");
            _goBackButton = Query<Button>("btn-back");
        }

        protected override void RegisterCallbacks()
        {
            _startOnboardingButton.clicked += ShowOnboarding;
            _goBackButton.clicked += ShowWelcomeHub;
            
            _root.RegisterCallback<TransitionEndEvent>(evt =>
            {
                if (evt.target != evt.currentTarget)
                    return;

                if (_root.ClassListContains("popup-overlay-hidden"))
                {
                    //_root.style.visibility = Visibility.Hidden;
                    _root.pickingMode = PickingMode.Ignore;
                }
            });

            _root.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target != evt.currentTarget)
                    return;

                Hide();
            });
        }

        public override void Show()
        {
            //_currentSlide = 0;
            //LeftArrowClicked();

            _root.pickingMode = PickingMode.Position;
            _root.RemoveFromClassList("popup-overlay-hidden");
            _root.AddToClassList("popup-overlay-shown");
        }

        public override void Hide()
        {
            _root.RemoveFromClassList("popup-overlay-shown");
            _root.AddToClassList("popup-overlay-hidden");
        }

        private void ShowWelcomeHub()
        {
            _onboardingView.Hide();
            _welcomeHubView.Show();
        }

        private void ShowOnboarding()
        {
            _welcomeHubView.Hide();
            _onboardingView.Show();
        }
    }
}