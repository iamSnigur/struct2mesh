using UnityEngine.Assertions;
using UnityEngine.UIElements;
using VContainer.Unity;

namespace UI.Components
{
    public abstract class ViewComponent : IStartable
    {
        protected readonly IUIRoot _uiRoot;
        protected VisualElement _viewRoot;
        private readonly string _viewRootSelector;

        protected ViewComponent(IUIRoot root, string viewRootSelector)
        {
            _uiRoot = root;
            _viewRootSelector = viewRootSelector;
        }

        public virtual void Start()
        {
            _viewRoot = _uiRoot.Root.Q(_viewRootSelector);
            QueryElements();
            RegisterCallbacks();
        }

        protected virtual void QueryElements() { }
        protected virtual void RegisterCallbacks() { }

        public virtual void Show() => _viewRoot.style.display = DisplayStyle.Flex;
        public virtual void Hide() => _viewRoot.style.display = DisplayStyle.None;

        protected T Query<T>(string name) where T : VisualElement
        {
            T visualElement = _viewRoot.Q<T>(name);
            Assert.IsNotNull(
                visualElement,
                $"Visual element {name} not found in root {_viewRoot.name}");

            return visualElement;
        }
    }
}