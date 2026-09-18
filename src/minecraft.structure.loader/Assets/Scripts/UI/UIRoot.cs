using System.Collections;
using Core.AssetManagement;
using Logic;
using Logic.PlayerPreview;
using UI.Views;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UI
{
    [RequireComponent(typeof(UIDocument))]
    public class UIRoot : MonoBehaviour
    {
        public LoadingCurtainView LoadingCurtainView { get; private set; }
        
        private UIDocument _document;
        private MainView _mainView;
        private static IPanel s_rootPanel;

        public void InjectDependencies(
            CameraController cameraController,
            FileReceiver fileReceiver,
            StructureBuilder structureBuilder,
            PlayerPuppet playerPuppet)
        {
            _document = GetComponent<UIDocument>();
            s_rootPanel = _document
                .rootVisualElement
                .Q<VisualElement>(Selectors.UI_ROOT)
                .panel;
            
            _mainView = new MainView(
                _document.rootVisualElement,
                structureBuilder, 
                fileReceiver,
                cameraController,
                playerPuppet);
            
            LoadingCurtainView = new LoadingCurtainView(
                _document.rootVisualElement.Q<VisualElement>(Selectors.LOADING_CURTAIN_ROOT));
        }
        
        private IEnumerator Start()
        {
            while (!SplashScreen.isFinished || !MinecraftAssetProvider.Loaded)
                yield return null;

            _mainView.Show();
        }

        public static bool IsPointerOverUI()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            mousePos.y = Screen.height - mousePos.y;
            mousePos = RuntimePanelUtils.ScreenToPanel(s_rootPanel, mousePos);
            
            return s_rootPanel.Pick(mousePos) != null;
        }
    }
}