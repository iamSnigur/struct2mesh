using UI.Components;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace UI.Views
{
    public class LoadingCurtainView : ViewComponent, ILoadingCurtain
    {
        private VisualElement _progressBar;
        private Label _progressLabel;

        [Inject]
        public LoadingCurtainView(IUIRoot ui) : base(ui, Selectors.LOADING_CURTAIN_ROOT)
        { }

        public override void Start()
        {
            base.Start();
            SetProgressValue(0f);
        }

        protected override void QueryElements()
        {
            _progressBar = Query<VisualElement>(Selectors.PROGRESS_BAR);
            _progressLabel = Query<Label>(Selectors.LOADER_LABEL);
        }

        protected override void RegisterCallbacks()
        {
            _viewRoot.RegisterCallback<TransitionEndEvent>(evt =>
            {
                if (evt.target != evt.currentTarget)
                    return;

                if (_viewRoot.ClassListContains(Selectors.LOADER_HIDDEN)) 
                    _viewRoot.pickingMode = PickingMode.Ignore;
            });
        }

        public void SetProgressText(string text) => 
            _progressLabel.text = text;

        public void SetProgressValue(float value)
        {
            value = Mathf.Clamp(value, 0f, 100f);
            _progressBar.style.width = Length.Percent(value);
        }

        public override void Show()
        {
            _viewRoot.pickingMode = PickingMode.Position;
            _viewRoot.style.display = DisplayStyle.Flex;
            _viewRoot.RemoveFromClassList(Selectors.LOADER_HIDDEN);
            _viewRoot.AddToClassList(Selectors.LOADER_SHOWN);
        }

        public override void Hide()
        {
            _viewRoot.RemoveFromClassList(Selectors.LOADER_SHOWN);
            _viewRoot.AddToClassList(Selectors.LOADER_HIDDEN);
        }
    }
}