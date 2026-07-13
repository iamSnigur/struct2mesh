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
            _okButton = Query<Button>("btn-ok");
        }

        protected override void RegisterCallbacks()
        {
            _okButton.clicked += Hide;
            
            _root.RegisterCallback<TransitionEndEvent>(evt =>
            {
                if (evt.target != evt.currentTarget)
                    return;
                
                if (_root.ClassListContains("disclaimer-hidden"))
                    _root.style.display = DisplayStyle.None;
            });
        }

        public override void Show()
        {
            _root.style.display = DisplayStyle.Flex;
            _root.RemoveFromClassList("disclaimer-hidden");
            _root.AddToClassList("disclaimer-shown");
        }

        public override void Hide()
        {
            _root.RemoveFromClassList("disclaimer-shown");
            _root.AddToClassList("disclaimer-hidden");
        }
    }
}