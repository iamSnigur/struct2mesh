using UI.Components;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class DisclaimerView : ViewComponent
    {
        private Button _okButton;
        
        public DisclaimerView(VisualElement root) : base(root)
        { }

        protected override void QueryElements()
        {
            _okButton = Query<Button>(Selectors.DISCLAIMER_BTN_OK);
        }

        protected override void RegisterCallbacks()
        {
            _okButton.clicked += Hide;
            
            _root.RegisterCallback<TransitionEndEvent>(evt =>
            {
                if (evt.target != evt.currentTarget)
                    return;
                
                if (_root.ClassListContains(Selectors.DISCLAIMER_HIDDEN))
                    _root.style.display = DisplayStyle.None;
            });
        }

        public override void Show()
        {
            _root.style.display = DisplayStyle.Flex;
            _root.RemoveFromClassList(Selectors.DISCLAIMER_HIDDEN);
            _root.AddToClassList(Selectors.DISCLAIMER_SHOWN);
        }

        public override void Hide()
        {
            _root.RemoveFromClassList(Selectors.DISCLAIMER_SHOWN);
            _root.AddToClassList(Selectors.DISCLAIMER_HIDDEN);
        }
    }
}