using System.Runtime.InteropServices;

namespace WebBridges
{
    public static class DragAndDropBridge
    {
        [DllImport("__Internal")]
        private static extern void OpenUploadPanelInternal();

        [DllImport("__Internal")]
        private static extern void CloseUploadPanelInternal();

        public static void OpenUploadPanel()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            OpenUploadPanelInternal();
#endif
        }
        
        public static void CloseUploadPanel()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CloseUploadPanelInternal();
#endif
        }
    }
}