using Logic.PlayerPreview;
using UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class WelcomeHubView : ViewComponent
    {
        private const string GITHUB_LINK = "https://github.com/iamSnigur";
        private const string SUPPORT_LINK = "https://buymeacoffee.com/iamsnigur";
        private const string REPORT_ISSUE_LINK = "https://forms.gle/Ujksc3g8e6U7pJHx6";
        
        private readonly VisualElement _overlayRoot;
        private readonly PlayerPreviewController _playerPreviewController;
        
        private Label _githubLabel;
        private Label _supportLabel;
        private Label _reportIssueLabel;
        private VisualElement _parallaxMedia;
        private VisualElement _parallaxViewport;
        
        public WelcomeHubView(
            VisualElement root,
            VisualElement overlayRoot,
            PlayerPreviewController playerPreviewController) : base(root)
        {
            _overlayRoot = overlayRoot;
            _playerPreviewController = playerPreviewController;
            _playerPreviewController.Init();
            Initialize();

            _parallaxMedia.style.backgroundImage =
                new StyleBackground(
                    Background.FromRenderTexture(_playerPreviewController.RenderTexture));
        }

        protected override void QueryElements()
        {
            _parallaxViewport = Query<VisualElement>(Selectors.PARALLAX_VIEWPORT);
            _parallaxMedia = Query<VisualElement>(Selectors.PARALLAX_MEDIA);
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

        public void TogglePuppetCamera(bool active) => _playerPreviewController.ToggleCamera(active);
        public void ShowPuppet() => _playerPreviewController.Show();
        public void HidePuppet() => _playerPreviewController.Hide();
        
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
            _playerPreviewController.Puppet.Punch();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            Rect b = _parallaxViewport.worldBound;
            Vector2 uv = new(
                (evt.position.x - b.xMin) / b.width,
                1f - (evt.position.y - b.yMin) / b.height);
            _playerPreviewController.Puppet.LookAt(uv);
        }
    }
}