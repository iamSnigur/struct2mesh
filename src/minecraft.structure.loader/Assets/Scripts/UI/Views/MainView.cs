using Logic;
using Logic.BuildInfo;
using UI.Components;
using UnityEngine.UIElements;
using VContainer;
using WebBridges;

namespace UI.Views 
{
    public class MainView : ViewComponent
    {
        private readonly DisclaimerView _disclaimerView;
        private readonly OverlayPopupView _overlayPopupView;
        private readonly SidebarView _sidebarView;
        private readonly ToastComponent _toastComponent;
        private readonly IBuildInfoService _buildInfoService;

        private Label _fileNameLabel;
        private Label _versionLabel;
        private Button _openPopupButton;

        [Inject]
        public MainView(
            IUIRoot ui,
            OverlayPopupView overlayPopupView,
            SidebarView sidebarView,
            DisclaimerView disclaimerView,
            ToastComponent toastComponent,
            FileReceiver fileReceiver,
            IBuildInfoService buildInfoService) 
            : base(ui, Selectors.UI_ROOT)
        {
            _overlayPopupView = overlayPopupView;
            _sidebarView = sidebarView;
            _disclaimerView = disclaimerView;
            _toastComponent = toastComponent;
            _buildInfoService = buildInfoService;

            fileReceiver.OnFileReceived += OnFileReceived;
        }

        public override void Start()
        {
            base.Start();
            
            SetVersion(_buildInfoService.BuildInfo);
#if UNITY_WEBGL && !UNITY_EDITOR
            ClipboardBridge.AttachCopyOverlay(_versionLabel, _buildInfoService.BuildInfo);
#endif
        }

        protected override void QueryElements()
        {
            _fileNameLabel = Query<Label>(Selectors.FILE_NAME_LABEL);
            _versionLabel = Query<Label>(Selectors.VERSION_LABEL);
            _openPopupButton = Query<Button>(Selectors.BTN_WELCOME_HUB);
        }

        protected override void RegisterCallbacks()
        {
            _openPopupButton.clicked += () => _overlayPopupView.Show();
            _versionLabel.RegisterCallback<ClickEvent>(OnVersionLabelClicked);
        }

        public override void Show()
        {
            _disclaimerView.Show();
            _sidebarView.OpenUploadOverlay();
        }

        private void OnVersionLabelClicked(ClickEvent evt)
        {
            ClipboardBridge.CopyToClipboard(_versionLabel.text);
            _toastComponent.PlayAnimation(evt);
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