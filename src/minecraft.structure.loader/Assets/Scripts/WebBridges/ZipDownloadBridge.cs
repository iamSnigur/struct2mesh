using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace WebBridges
{
    public static class ZipDownloadBridge
    {
        [DllImport("__Internal")]
        private static extern void DownloadFileInternal(string path, byte[] data, int length);

        public static void DownloadFile(string path, byte[] data, int length)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            DownloadFileInternal(path, data, length);
#elif UNITY_EDITOR && UNITY_EDITOR_WIN
            File.WriteAllBytes(Path.Combine(Application.dataPath, $"../{path}"), data);
#endif
        }
    }
}