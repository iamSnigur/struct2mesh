using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;

namespace UI
{
    [CreateAssetMenu(fileName = "AnimationConfig", menuName = "SpriteAnimator/AnimationConfig")]
    public class SpriteAnimationConfig : ScriptableObject
    {
        public bool Loop;
        public float FPS;
        public Texture2D[] Frames;
    }
    
    public class SpriteAnimator : MonoBehaviour
    {
        public SpriteAtlas Atlas;
        public SpriteAnimationConfig AnimationConfig;
        public string ElementName;

        private IVisualElementScheduledItem _scheduledAnimation;
        private VisualElement _r;
        private Sprite[] _frames;
        private int _currentFrame;
        
        private void Awake()
        {
            _r = GetComponent<UIDocument>()
                .rootVisualElement
                .Q<VisualElement>("pool-animation");
            
            _frames = new Sprite[150];
            for (int i = 0; i < 150; i++)
            {
                _frames[i] = Atlas.GetSprite($"ezgif-frame-{(i + 1):000}");
            }

            _scheduledAnimation = _r.schedule.Execute(UpdateFrame).Every(1000 / 15);
        }

        public void StartAnimation()
        {
            if (_scheduledAnimation?.isActive ?? false)
                _scheduledAnimation.Resume();
            else
                _scheduledAnimation = _r.schedule.Execute(UpdateFrame).Every(1000 / 15);

        }

        public void PauseAnimation()
        {
            _scheduledAnimation?.Pause();
        }
        
        private void UpdateFrame()
        {
            _r.style.backgroundImage = new StyleBackground(_frames[_currentFrame]);
            _currentFrame = (_currentFrame + 1) % 150;            
        }
    }
}