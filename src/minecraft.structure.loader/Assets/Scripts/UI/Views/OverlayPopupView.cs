using Logic.PlayerPreview;
using UI.Components;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class OverlayPopupView : ViewComponent
    {
        private readonly WelcomeHubView _welcomeHubView;
        private readonly KnowledgeBaseView _knowledgeBaseView;

        private Button _showKnowledgeBaseButton;
        private Button _goBackButton;
        
        public OverlayPopupView(VisualElement root, PlayerPuppet playerPuppet) : base(root)
        {
            _welcomeHubView = new WelcomeHubView(
                Query<VisualElement>(Selectors.WELCOME_HUB_ROOT), 
                _root,
                playerPuppet);
            _knowledgeBaseView = new KnowledgeBaseView(
                Query<VisualElement>(Selectors.KNOWLEDGE_BASE_ROOT));
            
            _root.pickingMode = PickingMode.Ignore;
            
            Initialize();
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
            
            _root.RegisterCallback<TransitionEndEvent>(evt =>
            {
                if (evt.target != evt.currentTarget)
                    return;

                if (_root.ClassListContains(Selectors.POPUP_OVERLAY_HIDDEN)) 
                    _root.pickingMode = PickingMode.Ignore;
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
            ShowWelcomeHub();
            _welcomeHubView.ShowPuppet();
            _root.pickingMode = PickingMode.Position;
            _root.RemoveFromClassList(Selectors.POPUP_OVERLAY_HIDDEN);
            _root.AddToClassList(Selectors.POPUP_OVERLAY_SHOWN);
        }

        public override void Hide()
        {
            _welcomeHubView.HidePuppet();
            _root.RemoveFromClassList(Selectors.POPUP_OVERLAY_SHOWN);
            _root.AddToClassList(Selectors.POPUP_OVERLAY_HIDDEN);
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