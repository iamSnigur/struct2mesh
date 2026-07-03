using UnityEngine;

namespace Core.UI
{
    [CreateAssetMenu(fileName = "OnboardingSlidesConfig")]
    public class OnboardingSlidesConfig : ScriptableObject
    {
        public OnboardingSlide[] Slides;
    }
}