using Logic.PlayerPreview;
using UI.Components;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace UI.Views
{
    public class WelcomeHubView : ViewComponent
    {
        private const string GITHUB_LINK = "https://github.com/iamSnigur";
        private const string SUPPORT_LINK = "https://buymeacoffee.com/iamsnigur";
        private const string REPORT_ISSUE_LINK = "https://forms.gle/Ujksc3g8e6U7pJHx6";
        
        private readonly IPlayerPreview _playerPreview;
        
        private VisualElement _overlayRoot;
        private Label _githubLabel;
        private Label _supportLabel;
        private Label _reportIssueLabel;
        private VisualElement _playerPreviewElement;
        
        [Inject]
        public WelcomeHubView(
            IUIRoot ui,
            IPlayerPreview playerPreview) 
            : base(ui, Selectors.WELCOME_HUB_ROOT)
        {
            _playerPreview = playerPreview;
        }

        public override void Start()
        {
            _overlayRoot = _uiRoot.Root.Q(Selectors.POPUP_OVERLAY_ROOT);
            base.Start();
            _playerPreviewElement.style.backgroundImage =
                new StyleBackground(Background.FromRenderTexture(_playerPreview.RenderTexture));
        }

        protected override void QueryElements()
        {
            _playerPreviewElement = Query<VisualElement>(Selectors.PLAYER_PREVIEW);
            _githubLabel = Query<Label>(Selectors.GITHUB_LINK);
            _supportLabel = Query<Label>(Selectors.SUPPORT_LINK);
            _reportIssueLabel = Query<Label>(Selectors.REPORT_ISSUE_LINK);
        }

        protected override void RegisterCallbacks()
        {
            _githubLabel.RegisterCallback<ClickEvent>(_ => OpenLink(GITHUB_LINK));
            _supportLabel.RegisterCallback<ClickEvent>(_ => OpenLink(SUPPORT_LINK));
            _reportIssueLabel.RegisterCallback<ClickEvent>(_ => OpenLink(REPORT_ISSUE_LINK));
        }

        private static void OpenLink(string url) => 
            Application.OpenURL(url);

        public void SetPuppetCameraActive(bool active) => _playerPreview.SetCameraActive(active);
        public void ShowPuppet() => _playerPreview.Show();
        public void HidePuppet() => _playerPreview.Hide();
        
        public override void Show()
        {
            base.Show();
            _overlayRoot.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _overlayRoot.RegisterCallback<ClickEvent>(OnPointerClick);
        }

        public override void Hide()
        {
            base.Hide();
            _overlayRoot.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _overlayRoot.UnregisterCallback<ClickEvent>(OnPointerClick);
        }

        private void OnPointerClick(ClickEvent evt)
        {
            _playerPreview.Punch();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            Rect bound = _playerPreviewElement.worldBound;
            Vector2 screenPos = new(
                (evt.position.x - bound.xMin) / bound.width,
                1f - (evt.position.y - bound.yMin) / bound.height);
            
            _playerPreview.LookAt(screenPos);
        }
    }
}