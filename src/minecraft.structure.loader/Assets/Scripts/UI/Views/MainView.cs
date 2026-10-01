using Core;
using Logic;
using UI.Components;
using UnityEngine;
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

        private Label _fileNameLabel;
        private Label _versionLabel;
        private Button _openPopupButton;
        private Label _copiedToast;
        private bool _isToastPlaying;
        private IVisualElementScheduledItem _hideItem;
        private IVisualElementScheduledItem _showItem;
        private IVisualElementScheduledItem _resetItem;

        [Inject]
        public MainView(
            IUIRoot ui,
            OverlayPopupView overlayPopupView,
            SidebarView sidebarView,
            DisclaimerView disclaimerView,
            FileReceiver fileReceiver) 
            : base(ui, Selectors.UI_ROOT)
        {
            _overlayPopupView = overlayPopupView;
            _sidebarView = sidebarView;
            _disclaimerView = disclaimerView;
            
            fileReceiver.OnFileReceived += OnFileReceived;
        }

        public override void Start()
        {
            base.Start();
            
            // move to separate service
            BuildInfo.Init();
            SetVersion(BuildInfo.FullInfo);
#if UNITY_WEBGL && !UNITY_EDITOR
            ClipboardBridge.AttachCopyOverlay(_versionLabel, BuildInfo.FullInfo);
#endif
        }

        protected override void QueryElements()
        {
            _fileNameLabel = Query<Label>(Selectors.FILE_NAME_LABEL);
            _versionLabel = Query<Label>(Selectors.VERSION_LABEL);
            _copiedToast = Query<Label>(Selectors.COPIED_TOAST);
            _openPopupButton = Query<Button>(Selectors.BTN_WELCOME_HUB);
        }

        protected override void RegisterCallbacks()
        {
            _openPopupButton.clicked += () => _overlayPopupView.Show();
            _versionLabel.RegisterCallback<ClickEvent>(OnVersionLabelClicked);
                
            _showItem = _copiedToast.schedule
                .Execute(() => _copiedToast.AddToClassList(Selectors.COPIED_TOAST_SHOWN));

            _hideItem = _copiedToast.schedule
                .Execute(() => _copiedToast.AddToClassList(Selectors.COPIED_TOAST_HIDDEN));

            _resetItem = _copiedToast.schedule.Execute(ResetToast);
            
            _showItem.Pause();
            _hideItem.Pause();
            _resetItem.Pause();
        }

        public override void Show()
        {
            _disclaimerView.Show();
            _sidebarView.OpenUploadOverlay();
        }

        private void OnVersionLabelClicked(ClickEvent evt)
        {
            ClipboardBridge.CopyToClipboard(_versionLabel.text);
            PlayToastAnimation(evt);
        }

        // remove toast logic from MainView to ToastComponent
        private void PlayToastAnimation(ClickEvent evt)
        {
            if (_isToastPlaying)
                return;
            
            ResetToast();
            _isToastPlaying = true;
            _copiedToast.style.left = CalculateOffset(evt);
            _showItem.ExecuteLater(16);
            _hideItem.ExecuteLater(800);
            _resetItem.ExecuteLater(1400);
        }

        private void ResetToast()
        {
            _isToastPlaying = false;
            _copiedToast.RemoveFromClassList(Selectors.COPIED_TOAST_SHOWN);
            _copiedToast.RemoveFromClassList(Selectors.COPIED_TOAST_HIDDEN);
        }

        private float CalculateOffset(ClickEvent evt)
        {
            float halfWidth = _copiedToast.resolvedStyle.width / 2f;
            return Mathf.Max(evt.localPosition.x - halfWidth, 0f);
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