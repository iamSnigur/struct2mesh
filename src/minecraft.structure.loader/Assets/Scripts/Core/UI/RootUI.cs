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
    public class RootUI : MonoBehaviour
    {
        [DllImport("__Internal")]
        private static extern void OpenUploadPanel();

        [DllImport("__Internal")]
        private static extern void CloseUploadPanel();

        private CameraController _cameraController;
        private FileReceiver _fileReceiver;
        private NbtStructureParser _nbtStructureParser;
        
        private UIDocument _doc;
        private VisualElement _root;

        public void InjectDependencies(
            CameraController cameraController,
            FileReceiver fileReceiver,
            NbtStructureParser nbtStructureParser)
        {
            _cameraController = cameraController;
            _fileReceiver = fileReceiver;
            _nbtStructureParser = nbtStructureParser;
        }
        
        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;
            
            BindExportButtons();
            BindButtons();
            
            BuildInfo.Init();
            SetVersion(BuildInfo.FullInfo);
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

        private void BindExportButtons()
        {
            // put objects into factory or what
            Button exportFbx = _root.Q<Button>("btn-export-fbx");
            exportFbx.clicked += () => OnExportClicked(new FbxExporter());
            
            Button exportGlb = _root.Q<Button>("btn-export-glb");
            exportGlb.clicked += () => OnExportClicked(new GlbExporter());
            
            Button exportObj = _root.Q<Button>("btn-export-obj");
            exportObj.clicked += () => OnExportClicked(new ObjExporter());
            
            Button exportPly = _root.Q<Button>("btn-export-ply");
            exportPly.clicked += () => OnExportClicked(new PlyExporter());
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
            ModelExporter.ExportToZip(lbl.text.ToLower(), _nbtStructureParser.Root, exportStrategy);
        }

        private void OnLoadNewClicked() => 
            OpenUploadPanel();

        private void OnCameraResetClicked() => 
            _cameraController.ResetPivot();

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