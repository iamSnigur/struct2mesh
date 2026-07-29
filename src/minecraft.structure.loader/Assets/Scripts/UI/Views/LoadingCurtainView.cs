using Cysharp.Threading.Tasks;
using UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class LoadingCurtainView : ViewComponent
    {
        private VisualElement _loaderIcon;
        private Label _progressLabel;

        private bool _isActive;

        public LoadingCurtainView(VisualElement root) : base(root)
        {
            Initialize();
        }

        protected override void QueryElements()
        {
            _loaderIcon = Query<VisualElement>(Selectors.LOADER_ICON);
            _progressLabel = Query<Label>(Selectors.LOADER_LABEL);
        }

        public void SetProgressText(string text) => 
            _progressLabel.text = text;

        public override void Show()
        {
            _isActive = true;
            _root.style.display = DisplayStyle.Flex;
            _root.style.opacity = 1f;
            DoSpinnerAnimation(0.9f).Forget();
        }

        public override void Hide() => 
            FadeOut(0.3f).Forget();

        private async UniTaskVoid FadeOut(float duration = 1f)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;

                _root.style.opacity = 1f - t;

                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }

            _root.style.opacity = 0f;
            _isActive = false;
            _root.style.display = DisplayStyle.None;
        }
        
        private async UniTaskVoid DoSpinnerAnimation(float cycleDuration = 1f)
        {
            _loaderIcon.style.scale = new StyleScale(Vector2.one);
            _loaderIcon.style.rotate = new StyleRotate(new Rotate(0f));
            float time = 0f;
            
            while (_isActive)
            {
                float normalizedTime = time / cycleDuration;
                float angle = SpringEase(normalizedTime) * 360f;
                _loaderIcon.style.rotate = new StyleRotate(new Rotate(angle));
                await UniTask.Yield();

                time += Time.deltaTime;

                if (time > cycleDuration)
                    time = 0f;
            }   
        }
        
        private static float SpringEase(float t)
        {
            t = Mathf.Clamp01(t);

            float decay = Mathf.Pow(2f, -10f * t);
            float oscillation = Mathf.Sin((t - 0.175f) * 10f);

            return decay * oscillation;
        }
    }
}