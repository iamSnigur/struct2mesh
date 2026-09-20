using UI.Components;
using UnityEngine.UIElements;
using VContainer;

namespace UI.Views
{
    public class OverlayPopupView : ViewComponent
    {
        private readonly WelcomeHubView _welcomeHubView;
        private readonly KnowledgeBaseView _knowledgeBaseView;

        private Button _showKnowledgeBaseButton;
        private Button _goBackButton;
        
        [Inject]
        public OverlayPopupView(
            IUIRoot ui,
            WelcomeHubView welcomeHubView,
            KnowledgeBaseView knowledgeBaseView) 
            : base(ui, Selectors.POPUP_OVERLAY_ROOT)
        {
            _welcomeHubView = welcomeHubView;
            _knowledgeBaseView = knowledgeBaseView;
        }

        public override void Start()
        {
            base.Start();
            _viewRoot.pickingMode = PickingMode.Ignore;
        }

        protected override void QueryElements()
        {
            _showKnowledgeBaseButton = Query<Button>(Selectors.BTN_SHOW_KNOWLEDGE_BASE);
            _goBackButton = Query<Button>(Selectors.BTN_BACK_TO_WELCOME_HUB);
        }

        protected override void RegisterCallbacks()
        {
            _showKnowledgeBaseButton.clicked += ShowKnowledgeBase;
            _goBackButton.clicked += ShowWelcomeHub;
            
            _viewRoot.RegisterCallback<TransitionEndEvent>(evt =>
            {
                if (evt.target != evt.currentTarget)
                    return;

                if (_viewRoot.ClassListContains(Selectors.POPUP_OVERLAY_HIDDEN))
                {
                    _viewRoot.pickingMode = PickingMode.Ignore;
                    _welcomeHubView.SetPuppetCameraActive(false);
                }
            });

            _viewRoot.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target != evt.currentTarget)
                    return;

                Hide();
            });
        }

        public override void Show()
        {
            ShowWelcomeHub();
            _welcomeHubView.SetPuppetCameraActive(true);
            _welcomeHubView.ShowPuppet();
            _viewRoot.pickingMode = PickingMode.Position;
            _viewRoot.RemoveFromClassList(Selectors.POPUP_OVERLAY_HIDDEN);
            _viewRoot.AddToClassList(Selectors.POPUP_OVERLAY_SHOWN);
        }

        public override void Hide()
        {
            _welcomeHubView.HidePuppet();
            _viewRoot.RemoveFromClassList(Selectors.POPUP_OVERLAY_SHOWN);
            _viewRoot.AddToClassList(Selectors.POPUP_OVERLAY_HIDDEN);
        }

        private void ShowWelcomeHub()
        {
            _knowledgeBaseView.Hide();
            _welcomeHubView.Show();
        }

        private void ShowKnowledgeBase()
        {
            _welcomeHubView.Hide();
            _knowledgeBaseView.Show();
        }
    }
}