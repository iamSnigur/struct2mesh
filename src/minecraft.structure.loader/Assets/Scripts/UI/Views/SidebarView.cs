using Logic;
using Logic.Exporter;
using UI.Components;
using UnityEngine.UIElements;
using VContainer;
using WebBridges;

namespace UI.Views
{
    public class SidebarView : ViewComponent
    {
        private readonly ExporterRegistry _exporterRegistry;
        private readonly StructureBuilder _structureBuilder;
        private readonly CameraController _cameraController;
        
        private Label _fileNameLabel;
        
        private Label _exportErrorLabel;
        private Button _exportFbxButton;
        private Button _exportGlbButton;
        private Button _exportObjButton;
        private Button _exportPlyButton;
        private Button _loadNewButton;
        private Button _cameraResetButton;
        
        [Inject]
        public SidebarView(
            IUIRoot ui,
            StructureBuilder structureBuilder,
            CameraController cameraController) 
            : base(ui, Selectors.SIDEBAR_ROOT)
        {
            _exporterRegistry = new ExporterRegistry();
            _structureBuilder = structureBuilder;
            _cameraController = cameraController;
        }

        public override void Start()
        {
            base.Start();
            _fileNameLabel = _uiRoot.Root.Q<Label>(Selectors.FILE_NAME_LABEL);
        }

        protected override void QueryElements()
        {
            _exportErrorLabel = Query<Label>(Selectors.EXPORT_ERROR);

            _exportFbxButton = Query<Button>(Selectors.BTN_EXPORT_ASCII_FBX);
            _exportGlbButton = Query<Button>(Selectors.BTN_EXPORT_GLB);
            _exportObjButton = Query<Button>(Selectors.BTN_EXPORT_OBJ);
            _exportPlyButton = Query<Button>(Selectors.BTN_EXPORT_PLY);
            
            _cameraResetButton = Query<Button>(Selectors.BTN_CAMERA_RESET);
            _loadNewButton = Query<Button>(Selectors.BTN_LOAD_NEW);
        }

        protected override void RegisterCallbacks()
        {
            _exportFbxButton.clicked += () => OnExportClicked(ExportType.AsciiFbx);
            _exportGlbButton.clicked += () => OnExportClicked(ExportType.Glb);
            _exportObjButton.clicked += () => OnExportClicked(ExportType.Obj);
            _exportPlyButton.clicked += () => OnExportClicked(ExportType.Ply);

            _loadNewButton.clicked += OpenUploadOverlay;
            _cameraResetButton.clicked += () => _cameraController.ResetPivot();
        }

        public void ToggleExportError(bool show) =>
            _exportErrorLabel.style.visibility = show 
                ? Visibility.Visible
                : Visibility.Hidden;
        
        public void OpenUploadOverlay() => 
            DragAndDropBridge.OpenUploadPanel();
        
        public void CloseUploadOverlay() => 
            DragAndDropBridge.CloseUploadPanel();
        
        private void OnExportClicked(ExportType exportType)
        {
            if (!_structureBuilder.IsStructureLoaded)
            {
                ToggleExportError(true);
                return;
            }
            
            ToggleExportError(false);
            
            IExporter exporter = _exporterRegistry.GetFor(exportType);
            ModelExporter.ExportToZip(
                _fileNameLabel.text.ToLower(),
                _structureBuilder.Root,
                exporter);
        }
    }
}