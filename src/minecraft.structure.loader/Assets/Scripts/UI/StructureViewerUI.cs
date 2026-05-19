using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    [RequireComponent(typeof(UIDocument))]
    public class StructureViewerUI : MonoBehaviour
    {
        [DllImport("__Internal")]
        private static extern void OpenUploadPanel();

        [DllImport("__Internal")]
        private static extern void CloseUploadPanel();

        public CameraController CameraController;
        public FileReceiver FileReceiver;
        
        private UIDocument _doc;
        private VisualElement _root;
        
        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator Start()
        {
            while (!SplashScreen.isFinished)
                yield return null;
            
            OpenUploadPanel();
            FileReceiver.OnFileReceived += OnFileReceived;
        }
        
        private void OnFileReceived(string fileName)
        {
            SetFileName(fileName);
            CloseUploadPanel();
        }
#endif

        private void OnEnable()
        {
            BuildExportButtons();
            BindButtons();
        }

        private void BuildExportButtons()
        {
            VisualElement exportSection = _root.Q<VisualElement>("export-section");
            if (exportSection == null)
            {
                Debug.LogWarning("[StructureViewerUI] Could not find 'export-section'.");
                return;
            }

            List<Button> existingBtns = exportSection.Query<Button>(className: "btn-export").ToList();

            foreach (Button b in existingBtns)
                b.clicked += OnExportClicked;
        }

        private void BindButtons()
        {
            Button btnLoad = _root.Q<Button>("btn-load-new");
            Button btnCamera = _root.Q<Button>("btn-camera-reset");

            if (btnLoad != null) btnLoad.clicked += OnLoadNewClicked;
            if (btnCamera != null) btnCamera.clicked += OnCameraResetClicked;
        }

        private void OnExportClicked()
        {
            Debug.Log("[StructureViewerUI] Export button clicked.");
        }

        private void OnLoadNewClicked() => 
            OpenUploadPanel();

        private void OnCameraResetClicked() => 
            CameraController.ResetPivot();

        public void SetFileName(string fileName)
        {
            Label lbl = _root.Q<Label>("file-label");
            if (lbl != null) lbl.text = fileName.ToUpper();
        }

        public void SetVersion(string version)
        {
            Label lbl = _root.Q<Label>("version-label");
            if (lbl != null) lbl.text = version.ToUpper();
        }
    }
}