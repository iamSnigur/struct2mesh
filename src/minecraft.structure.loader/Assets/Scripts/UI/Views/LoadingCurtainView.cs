using UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class LoadingCurtainView : ViewComponent
    {
        private VisualElement _progressBar;
        private Label _progressLabel;

        public LoadingCurtainView(VisualElement root) : base(root)
        {
            Initialize();
            SetProgressBar(0f);
        }

        protected override void QueryElements()
        {
            _progressBar = Query<VisualElement>(Selectors.PROGRESS_BAR);
            _progressLabel = Query<Label>(Selectors.LOADER_LABEL);
        }

        protected override void RegisterCallbacks()
        {
            _root.RegisterCallback<TransitionEndEvent>(evt =>
            {
                if (evt.target != evt.currentTarget)
                    return;

                if (_root.ClassListContains(Selectors.LOADER_HIDDEN)) 
                    _root.pickingMode = PickingMode.Ignore;
            });
        }

        public void SetProgressText(string text) => 
            _progressLabel.text = text;

        public void SetProgressBar(float progress)
        {
            progress = Mathf.Clamp(progress, 0f, 100f);
            _progressBar.style.width = Length.Percent(progress);
        }

        public override void Show()
        {
            _root.pickingMode = PickingMode.Position;
            _root.style.display = DisplayStyle.Flex;
            _root.RemoveFromClassList(Selectors.LOADER_HIDDEN);
            _root.AddToClassList(Selectors.LOADER_SHOWN);
        }

        public override void Hide()
        {
            _root.RemoveFromClassList(Selectors.LOADER_SHOWN);
            _root.AddToClassList(Selectors.LOADER_HIDDEN);
        }
    }
}