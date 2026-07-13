using System.Runtime.InteropServices;
using Logic;
using Logic.Exporter;
using UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Views
{
    public class SidebarView : ViewComponent
    {
        [DllImport("__Internal")]
        private static extern void OpenUploadPanel();

        [DllImport("__Internal")]
        private static extern void CloseUploadPanel();
        
        private readonly ExporterRegistry _exporterRegistry;
        private readonly StructureBuilder _structureBuilder;
        private readonly CameraController _cameraController;
        private readonly Label _fileNameLabel;
        
        private Label _exportErrorLabel;
        private Button _exportFbxButton;
        private Button _exportGlbButton;
        private Button _exportObjButton;
        private Button _exportPlyButton;
        private Button _loadNewButton;
        private Button _cameraResetButton;
        
        public SidebarView(VisualElement root,
            Label fileNameLabel,
            StructureBuilder structureBuilder,
            CameraController cameraController) : base(root)
        {
            _exporterRegistry = new ExporterRegistry();
            _fileNameLabel = fileNameLabel;
            _structureBuilder = structureBuilder;
            _cameraController = cameraController;
        }

        protected override void QueryElements()
        {
            _exportErrorLabel = Query<Label>("export-error");

            _exportFbxButton = Query<Button>("btn-export-fbx");
            _exportGlbButton = Query<Button>("btn-export-glb");
            _exportObjButton = Query<Button>("btn-export-obj");
            _exportPlyButton = Query<Button>("btn-export-ply");
            
            _cameraResetButton = Query<Button>("btn-camera-reset");
            _loadNewButton = Query<Button>("btn-load-new");
        }

        protected override void RegisterCallbacks()
        {
            _exportFbxButton.clicked += () => OnExportClicked("fbx");
            _exportGlbButton.clicked += () => OnExportClicked("glb");
            _exportObjButton.clicked += () => OnExportClicked("obj");
            _exportPlyButton.clicked += () => OnExportClicked("ply");

            _loadNewButton.clicked += OpenUploadOverlay;
            _cameraResetButton.clicked += () => _cameraController.ResetPivot();
        }

        public void ToggleExportError(bool show) =>
            _exportErrorLabel.style.visibility = show 
                ? Visibility.Visible
                : Visibility.Hidden;
        
        public void OpenUploadOverlay() => 
            OpenUploadPanel();
        
        public void CloseUploadOverlay() => 
            CloseUploadPanel();
        
        private void OnExportClicked(string format)
        {
            if (!_structureBuilder.Root.TryGetComponent(out Renderer _))
            {
                ToggleExportError(true);
                return;
            }
            
            ToggleExportError(false);
            
            IExporter exporter = _exporterRegistry.GetFor(format);
            ModelExporter.ExportToZip(
                _fileNameLabel.text.ToLower(),
                _structureBuilder.Root,
                exporter);
        }
    }
}