namespace UI.Views
{
    public interface ILoadingCurtain
    {
        void SetInfoText(string text);
        void SetErrorText(string text);
        void SetProgressValue(float value);
        void Show();
        void Hide();
    }
}