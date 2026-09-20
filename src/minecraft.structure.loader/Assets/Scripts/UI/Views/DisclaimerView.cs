using UI.Components;
using UnityEngine.UIElements;
using VContainer;

namespace UI.Views
{
    public class DisclaimerView : ViewComponent
    {
        private Button _okButton;

        [Inject]
        public DisclaimerView(IUIRoot ui) : base(ui, Selectors.DISCLAIMER_ROOT)
        { }

        protected override void QueryElements()
        {
            _okButton = Query<Button>(Selectors.BTN_OK);
        }

        protected override void RegisterCallbacks()
        {
            _okButton.clicked += Hide;
            
            _viewRoot.RegisterCallback<TransitionEndEvent>(evt =>
            {
                if (evt.target != evt.currentTarget)
                    return;
                
                if (_viewRoot.ClassListContains(Selectors.DISCLAIMER_HIDDEN))
                    _viewRoot.style.display = DisplayStyle.None;
            });
        }

        public override void Show()
        {
            _viewRoot.style.display = DisplayStyle.Flex;
            _viewRoot.RemoveFromClassList(Selectors.DISCLAIMER_HIDDEN);
            _viewRoot.AddToClassList(Selectors.DISCLAIMER_SHOWN);
        }

        public override void Hide()
        {
            _viewRoot.RemoveFromClassList(Selectors.DISCLAIMER_SHOWN);
            _viewRoot.AddToClassList(Selectors.DISCLAIMER_HIDDEN);
        }
    }
}