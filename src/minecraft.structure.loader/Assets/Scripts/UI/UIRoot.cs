using System.Collections;
using Core.AssetManagement;
using Logic;
using UI.Views;
using UnityEngine;
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

        public void InjectDependencies(
            CameraController cameraController,
            FileReceiver fileReceiver,
            StructureBuilder structureBuilder)
        {
            _document = GetComponent<UIDocument>();
            _mainView = new MainView(
                _document.rootVisualElement,
                structureBuilder, 
                fileReceiver,
                cameraController);
            
            LoadingCurtainView = new LoadingCurtainView(
                _document.rootVisualElement.Q<VisualElement>(Selectors.LOADING_CURTAIN_ROOT));
        }
        
        private IEnumerator Start()
        {
            while (!SplashScreen.isFinished || !MinecraftAssetProvider.Loaded)
                yield return null;

            _mainView.Show();
        }
    }
}