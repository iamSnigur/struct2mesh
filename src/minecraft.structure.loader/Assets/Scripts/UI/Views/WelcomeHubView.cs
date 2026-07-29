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
        
        private Label _githubLabel;
        private Label _supportLabel;
        private Label _reportIssueLabel;
        private VisualElement _parallaxMedia;
        private VisualElement _parallaxViewport;
        
        public WelcomeHubView(VisualElement root, VisualElement overlayRoot) : base(root)
        {
            _overlayRoot = overlayRoot;
            Initialize();
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

        public override void Show()
        {
            base.Show();
            _overlayRoot.RegisterCallback<PointerMoveEvent>(ParallaxMedia);
        }

        public override void Hide()
        {
            base.Hide();
            _overlayRoot.UnregisterCallback<PointerMoveEvent>(ParallaxMedia);
        }

        private Vector2 _targetPosition;
        private Vector2 _currentPosition;
        
        private const float MAX_MEDIA_OFFSET = 40f;
        private const float LERP_SPEED = 4f;

        private void ParallaxMedia(PointerMoveEvent evt)
        {
            Vector2 offset = (Vector2)evt.position - _parallaxViewport.worldBound.center;

            Vector2 contentSize = _parallaxViewport.contentRect.size * 1.5f;
            Vector2 halfSize = contentSize * 0.5f;

            offset = Vector2.Max(-halfSize, Vector2.Min(offset, halfSize));
            _targetPosition = (offset / contentSize) * (-MAX_MEDIA_OFFSET * 2f);

            _currentPosition = Vector2.Lerp(_currentPosition, _targetPosition, Time.deltaTime * LERP_SPEED);
            _parallaxMedia.style.translate = new Translate(_currentPosition.x, _currentPosition.y);
        }

        private static void OpenLink(string url) => 
            Application.OpenURL(url);
    }
}