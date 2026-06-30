using System;
using Cysharp.Threading.Tasks;

namespace Core.UI
{
    using UnityEngine;
    using UnityEngine.UIElements;

    public class DisclaimerPopup : MonoBehaviour
    {
        public AnimationCurve ShowAnimationCurve;
        public AnimationCurve HideAnimationCurve;

        private float _popupHeight;
        private float _popupHideHeight;
        private VisualElement _popupRoot;
        private UIDocument _doc;

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _popupRoot = _doc.rootVisualElement.Q<VisualElement>("disclaimer-root");

            _popupRoot.style.display = DisplayStyle.Flex;
            _popupRoot.RegisterCallbackOnce<GeometryChangedEvent>(_ =>
            {
                _popupHeight = _popupRoot.resolvedStyle.height;
                _popupHideHeight = -_popupHeight - 100f;
                _popupRoot.style.display = DisplayStyle.None;
            });
            
            Button okBtn = _popupRoot.Q<Button>("btn-ok");
            okBtn.clicked += Hide;

            SetTopOffset(_popupHideHeight);
        }

        public void Show()
        {
            _popupRoot.style.display = DisplayStyle.Flex;
            Animate(ShowAnimationCurve, _popupHideHeight, 0f).Forget();
        }

        private void Hide()
        {
            Animate(HideAnimationCurve, 0f, _popupHideHeight, 0.5f,
                onComplete: () => { _popupRoot.style.display = DisplayStyle.None; }).Forget();
        }

        private async UniTaskVoid Animate(
            AnimationCurve curve,
            float from, float to,
            float duration = 1f,
            Action onComplete = null)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float normalized = elapsed / duration;
                float t = curve.Evaluate(normalized);
                SetTopOffset(Mathf.LerpUnclamped(from, to, t));
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }

            SetTopOffset(to);
            onComplete?.Invoke();
        }

        private void SetTopOffset(float top)
        {
            _popupRoot.style.top = top;
        }
    }
}