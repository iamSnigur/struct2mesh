using System.Runtime.InteropServices;

namespace WebBridges
{
    public static class ScrollWheelBridge
    {
        [DllImport("__Internal")]
        private static extern void InitScrollWheelInternal();

        [DllImport("__Internal")]
        private static extern float GetScrollDeltaInternal();

        static ScrollWheelBridge()
        {
            InitScrollWheelInternal();
        }
        
        public static float GetScrollDelta()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return GetScrollDeltaInternal();
#else
            return 0f;
#endif
        }
    }
}
