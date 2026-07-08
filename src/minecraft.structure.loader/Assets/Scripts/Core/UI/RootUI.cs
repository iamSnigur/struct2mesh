using System.Runtime.InteropServices;
using Core.Exporter;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using System.Collections;
using Core.AssetManagement;

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
        private StructureBuilder _structureBuilder;
        
        private ExporterRegistry _exporterRegistry;
        private UIDocument _doc;
        private VisualElement _root;

        public void InjectDependencies(
            CameraController cameraController,
            FileReceiver fileReceiver,
            StructureBuilder structureBuilder)
        {
            _cameraController = cameraController;
            _fileReceiver = fileReceiver;
            _structureBuilder = structureBuilder;
        }
        
        private void Awake()
        {
            _exporterRegistry = new ExporterRegistry();
            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;
            
            BindExportButtons();
            BindButtons();
            
            BuildInfo.Init();
            SetVersion(BuildInfo.FullInfo);
        }

        private IEnumerator Start()
        {
            while (!SplashScreen.isFinished || !MinecraftAssetProvider.Loaded)
                yield return null;

            FindFirstObjectByType<DisclaimerPopup>().Show();
            
#if UNITY_WEBGL && !UNITY_EDITOR
            OpenUploadPanel();
#endif
            _fileReceiver.OnFileReceived += OnFileReceived;
        }
        
        private void OnFileReceived(string fileName)
        {
            SetFileName(fileName);
            CloseUploadPanel();
        }

        private void BindExportButtons()
        {
            Button exportFbx = _root.Q<Button>("btn-export-fbx");
            exportFbx.clicked += () => OnExportClicked("fbx");
            
            Button exportGlb = _root.Q<Button>("btn-export-glb");
            exportGlb.clicked += () => OnExportClicked("glb");
            
            Button exportObj = _root.Q<Button>("btn-export-obj");
            exportObj.clicked += () => OnExportClicked("obj");
            
            Button exportPly = _root.Q<Button>("btn-export-ply");
            exportPly.clicked += () => OnExportClicked("ply");
        }

        private void BindButtons()
        {
            Button btnLoad = _root.Q<Button>("btn-load-new");
            btnLoad.clicked += OnLoadNewClicked;
            
            Button btnCamera = _root.Q<Button>("btn-camera-reset");
            btnCamera.clicked += OnCameraResetClicked;
        }

        private void OnExportClicked(string format)
        {
            IExporter exporter = _exporterRegistry.GetFor(format);
            Label lbl = _root.Q<Label>("file-label");
            ModelExporter.ExportToZip(lbl.text.ToLower(), _structureBuilder.Root, exporter);
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

        private void SetVersion(string version)
        {
            Label lbl = _root.Q<Label>("version-label");
            lbl.text = version;
        }
    }
}