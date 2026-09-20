namespace UI.Views
{
    public interface ILoadingCurtain
    {
        void SetProgressText(string text);
        void SetProgressValue(float value);
        void Show();
        void Hide();
    }
}