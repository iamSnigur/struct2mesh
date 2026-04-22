using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(Outline))]
    public class OutlineHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private readonly Color _selectColor = new(1,1,1,1);
        private readonly Color _deselectColor = new(0,0,0,1);
        private Outline _outline;
        
        private void Awake()
        {
            _outline = GetComponent<Outline>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _outline.effectColor = _selectColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _outline.effectColor = _deselectColor;
        }
    }
}