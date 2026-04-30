namespace RYHMÄROJEKTI;

public partial class LoadingPage : ContentPage
{
    public LoadingPage()
    {
        InitializeComponent();
    }

    public void UpdateStatus(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (StatusLabel != null)
                StatusLabel.Text = message;
        });
    }
}
