using UI.Components;
using VContainer;

namespace UI.Views
{
    public class KnowledgeBaseView : ViewComponent
    {
        [Inject]
        public KnowledgeBaseView(IUIRoot ui) : base(ui, Selectors.KNOWLEDGE_BASE_ROOT)
        { }
    }
}