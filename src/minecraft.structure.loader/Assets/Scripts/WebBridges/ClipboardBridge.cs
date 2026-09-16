using System.Runtime.InteropServices;
using UnityEngine;

namespace WebBridges
{
    public static class ClipboardBridge
    {
        [DllImport("__Internal")]
        private static extern void CopyToClipboardInternal(string text);

        public static void CopyToClipboard(string text)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CopyToClipboardInternal(text);
#elif UNITY_EDITOR && UNITY_EDITOR_WIN
            GUIUtility.systemCopyBuffer = text;
#endif
        }
    }
}