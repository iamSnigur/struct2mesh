using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UI.Components
{
    public abstract class ViewComponent
    {
        protected readonly VisualElement _root;

        protected ViewComponent(VisualElement root)
        {
            _root = root;
            
            QueryElements();
            RegisterCallbacks();
        }
        
        protected virtual void QueryElements() { }
        protected virtual void RegisterCallbacks() { }
        
        public virtual void Show() {}
        public virtual void Hide() {}

        protected T Query<T>(string name) where T : VisualElement
        {
            T visualElement = _root.Q<T>(name);
            Assert.IsNotNull(
                visualElement,
                $"Visual element {name} not found in root {_root.name}");

            return visualElement;
        }
    }
}