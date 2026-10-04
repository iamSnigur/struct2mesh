using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace UI
{
    [RequireComponent(typeof(UIDocument))]
    public class UIRoot : MonoBehaviour, IUIRoot
    {
        public VisualElement Root { get; private set; }
        
        private UIDocument _document;
        private static IPanel s_rootPanel;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            Root = _document.rootVisualElement;
            
            s_rootPanel = _document
                .rootVisualElement
                .Q<VisualElement>(Selectors.UI_ROOT)
                .panel;
        }
        
        public static bool IsPointerOverUI()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            mousePos.y = Screen.height - mousePos.y;
            mousePos = RuntimePanelUtils.ScreenToPanel(s_rootPanel, mousePos);
            
            return s_rootPanel.Pick(mousePos) != null;
        }
    }
}