namespace RYHMÄROJEKTI.views;
using System.Windows.Input;

public partial class PalveluPage : ContentPage
{
	public PalveluPage()
	{
		InitializeComponent();
	}

    async void MuokkaaPalveluClicked(object sender, EventArgs e)
    {
        // Use absolute routing for Shell routes (prefix with ///)
        await Shell.Current.GoToAsync("/PalveluPageEdit");
    }

}