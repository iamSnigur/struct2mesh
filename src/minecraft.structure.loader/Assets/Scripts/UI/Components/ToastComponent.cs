using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace UI.Components
{
    public class ToastComponent : ViewComponent
    {
        private bool _isToastPlaying;
        private IVisualElementScheduledItem _hideItem;
        private IVisualElementScheduledItem _showItem;
        private IVisualElementScheduledItem _resetItem;

        [Inject]
        public ToastComponent(IUIRoot ui) 
            : base(ui, Selectors.COPIED_TOAST_ROOT)
        { }

        protected override void RegisterCallbacks()
        {
            _showItem = _viewRoot.schedule
                .Execute(() => _viewRoot.AddToClassList(Selectors.COPIED_TOAST_SHOWN));

            _hideItem = _viewRoot.schedule
                .Execute(() => _viewRoot.AddToClassList(Selectors.COPIED_TOAST_HIDDEN));

            _resetItem = _viewRoot.schedule.Execute(ResetToast);
            
            _showItem.Pause();
            _hideItem.Pause();
            _resetItem.Pause();
        }

        public void PlayAnimation(ClickEvent evt)
        {
            if (_isToastPlaying)
                return;
            
            ResetToast();
            _isToastPlaying = true;
            _viewRoot.style.left = CalculateOffset(evt);
            _showItem.ExecuteLater(16);
            _hideItem.ExecuteLater(800);
            _resetItem.ExecuteLater(1400);
        }

        private void ResetToast()
        {
            _isToastPlaying = false;
            _viewRoot.RemoveFromClassList(Selectors.COPIED_TOAST_SHOWN);
            _viewRoot.RemoveFromClassList(Selectors.COPIED_TOAST_HIDDEN);
        }

        private float CalculateOffset(ClickEvent evt)
        {
            float halfWidth = _viewRoot.resolvedStyle.width / 2f;
            return Mathf.Max(evt.localPosition.x - halfWidth, 0f);
        }
    }
}