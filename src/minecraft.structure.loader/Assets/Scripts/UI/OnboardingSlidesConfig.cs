using UnityEngine;

namespace UI
{
    [CreateAssetMenu(fileName = "OnboardingSlidesConfig")]
    public class OnboardingSlidesConfig : ScriptableObject
    {
        public OnboardingSlide[] Slides;
    }
}