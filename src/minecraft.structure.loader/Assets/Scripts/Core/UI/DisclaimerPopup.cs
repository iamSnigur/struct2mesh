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

        private VisualElement _popupRoot;
        private UIDocument _doc;
        private Coroutine _animCoroutine;

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _popupRoot = _doc.rootVisualElement.Q<VisualElement>("disclaimer-root");

            Button okBtn = _popupRoot.Q<Button>("btn-ok");
            okBtn.clicked += Hide;

            SetTopOffset(-280);
        }

        public void Show()
        {
            _popupRoot.style.display = DisplayStyle.Flex;
            Animate(ShowAnimationCurve, -280, 0f).Forget();
        }

        private void Hide()
        {
            Animate(HideAnimationCurve, 0f, -280, 0.5f,
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