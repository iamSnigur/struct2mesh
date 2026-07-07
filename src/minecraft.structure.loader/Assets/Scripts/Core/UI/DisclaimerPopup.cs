using System.Linq;

namespace Core.UI
{
    using UnityEngine;
    using UnityEngine.UIElements;

    public class DisclaimerPopup : MonoBehaviour
    {
        private VisualElement _popupRoot;
        private UIDocument _doc;

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _popupRoot = _doc.rootVisualElement.Q<VisualElement>("disclaimer-root");
            
            _popupRoot.RegisterCallback<TransitionEndEvent>(evt =>
            {
                if (evt.target != evt.currentTarget)
                    return;
                
                if (_popupRoot.ClassListContains("disclaimer-hidden"))
                    _popupRoot.style.display = DisplayStyle.None;
            });
            
            Button okBtn = _popupRoot.Q<Button>("btn-ok");
            okBtn.clicked += Hide;
        }

        public void Show()
        {
            _popupRoot.style.display = DisplayStyle.Flex;
            _popupRoot.RemoveFromClassList("disclaimer-hidden");
            _popupRoot.AddToClassList("disclaimer-shown");
        }

        private void Hide()
        {
            _popupRoot.RemoveFromClassList("disclaimer-shown");
            _popupRoot.AddToClassList("disclaimer-hidden");
        }
    }
}