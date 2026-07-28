using UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class WelcomeHubView : ViewComponent
    {
        private Label _githubLabel;
        private Label _supportLabel;
        private Label _reportIssueLabel;
        private VisualElement _image;
        
        public WelcomeHubView(VisualElement root) : base(root)
        { }

        protected override void QueryElements()
        {
            _image = Query<Label>("");
            _githubLabel = Query<Label>("github-link");
            _supportLabel = Query<Label>("support-link");
            _reportIssueLabel = Query<Label>("feedback-link");
        }

        protected override void RegisterCallbacks()
        {
            _githubLabel.RegisterCallback<ClickEvent>(_ => OpenLink("https://github.com/iamSnigur"));
            _supportLabel.RegisterCallback<ClickEvent>(_ => OpenLink("https://buymeacoffee.com/iamsnigur"));
            _reportIssueLabel.RegisterCallback<ClickEvent>(_ => OpenLink("https://forms.gle/Ujksc3g8e6U7pJHx6"));
        }

        private static void OpenLink(string url) => 
            Application.OpenURL(url);

        public override void Show() => 
            _root.style.display = DisplayStyle.Flex;

        public override void Hide() => 
            _root.style.display = DisplayStyle.None;
    }
}