using System.Collections;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace UI
{
    public class UIController : MonoBehaviour
    {
        [DllImport("__Internal")]
        private static extern void OpenUploadPanel();
        
        [DllImport("__Internal")]
        private static extern void CloseUploadPanel();
        
        public GameObject UploadUI;
        public GameObject ExportUI;
        public FileReceiver FileReceiver;
        public TextMeshProUGUI FileName;

#if UNITY_WEBGL && !UNITY_EDITOR
      private IEnumerator Start()
        {
            while (!SplashScreen.isFinished)
                yield return null;
            
            //ExportUI.SetActive(false);
            //UploadUI.SetActive(true);
            OpenUploadPanel();
            FileReceiver.OnFileReceived += OnFileReceived;
        }  
#endif

        private void OnFileReceived(string fileName)
        {
            FileName.text = fileName;
            //ExportUI.SetActive(true);
            //UploadUI.SetActive(false);
            CloseUploadPanel();
        }
    }
}