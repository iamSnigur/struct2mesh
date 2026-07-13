using Core;
using Logic;
using UI.Components;
using UnityEngine.UIElements;

namespace UI.Views 
{
    public class MainView : ViewComponent
    {
        private readonly DisclaimerView _disclaimerView;
        private readonly OverlayPopupView _overlayPopupView;
        private readonly SidebarView _sidebarView;

        private Label _fileNameLabel;
        private Label _versionLabel;
        private Button _openPopupButton;

        public MainView(
            VisualElement root,
            StructureBuilder structureBuilder,
            FileReceiver fileReceiver,
            CameraController cameraController,
            OnboardingSlidesConfig slidesConfig) : base(root)
        {
            _overlayPopupView = new OverlayPopupView(
                Query<VisualElement>("popup-overlay"),
                slidesConfig);
            _sidebarView = new SidebarView(
                Query<VisualElement>("sidebar"),
                _fileNameLabel,
                structureBuilder,
                cameraController);
            
            _disclaimerView = new DisclaimerView(Query<VisualElement>("disclaimer-root"));
            
            BuildInfo.Init();
            SetVersion(BuildInfo.FullInfo);
            fileReceiver.OnFileReceived += OnFileReceived;
        }

        protected override void QueryElements()
        {
            _fileNameLabel = Query<Label>("file-label");
            _versionLabel = Query<Label>("version-label");
            _openPopupButton = Query<Button>("btn-welcome-hub");
        }

        protected override void RegisterCallbacks()
        {
            _openPopupButton.clicked += () => _overlayPopupView.Show();
        }

        public override void Show()
        {
            _disclaimerView.Show();
#if UNITY_WEBGL && !UNITY_EDITOR
            _sidebarView.OpenUploadOverlay();
#endif
        }

        private void OnFileReceived(string fileName)
        {
            SetFileName(fileName);
            _sidebarView.ToggleExportError(false);
            _sidebarView.CloseUploadOverlay();
        }

        private void SetFileName(string fileName) => 
            _fileNameLabel.text = fileName;

        private void SetVersion(string version) => 
            _versionLabel.text = version;
    }
}