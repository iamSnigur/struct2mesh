using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Core.UI
{
    public class LoadingCurtain : MonoBehaviour
    {
        private UIDocument _doc;
        private VisualElement _loader;
        private VisualElement _loaderIcon;
        private Label _progressLabel;

        private bool _isActive;
        
        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _loader = _doc.rootVisualElement.Q<VisualElement>("loader");
            _loaderIcon = _doc.rootVisualElement.Q<VisualElement>("loader-icon");
            _progressLabel = _doc.rootVisualElement.Q<Label>("loader-label");
        }

        public void SetProgressText(string text) => 
            _progressLabel.text = text;

        public void Show()
        {
            _isActive = true;
            _loader.style.display = DisplayStyle.Flex;
            _loader.style.opacity = 1f;
            DoSpinnerAnimation(0.9f).Forget();
        }

        public void Hide() => 
            FadeOut(0.3f).Forget();

        private async UniTaskVoid FadeOut(float duration = 1f)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;

                _loader.style.opacity = 1f - t;

                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }

            _loader.style.opacity = 0f;
            _isActive = false;
            _loader.style.display = DisplayStyle.None;
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