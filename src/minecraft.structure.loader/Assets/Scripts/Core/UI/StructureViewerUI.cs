using System.Collections.Generic;
using System.Runtime.InteropServices;
using Core.Exporter;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine.Rendering;
using System.Collections;
#endif

namespace Core.UI
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
        public NbtStructureParser NbtStructureParser;
        
        private UIDocument _doc;
        private VisualElement _root;
        
        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;
            BuildInfo.OnBuildInfoLoaded += SetVersion;
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
            BindExportButtons();
            BindButtons();
        }

        private void BindExportButtons()
        {
            // put objects into factory or what
            Button exportFbx = _root.Q<Button>("btn-export-fbx");
            exportFbx.clicked += () => OnExportClicked(new FbxExporter());
            
            Button exportGlb = _root.Q<Button>("btn-export-glb");
            exportGlb.clicked += () => OnExportClicked(new GlbExporter());
            
            Button exportObj = _root.Q<Button>("btn-export-obj");
            exportObj.clicked += () => OnExportClicked(new ObjExporter());
        }

        private void BindButtons()
        {
            Button btnLoad = _root.Q<Button>("btn-load-new");
            btnLoad.clicked += OnLoadNewClicked;
            
            Button btnCamera = _root.Q<Button>("btn-camera-reset");
            btnCamera.clicked += OnCameraResetClicked;
        }

        private void OnExportClicked(IExportStrategy exportStrategy)
        {
            Label lbl = _root.Q<Label>("file-label");
            StructureExporter.ExportToZip(lbl.text, NbtStructureParser.Root, exportStrategy);
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
            if (lbl != null) lbl.text = version;
        }
    }
}