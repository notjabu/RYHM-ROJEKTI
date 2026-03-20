namespace RYHMÄROJEKTI.views;
using System.Windows.Input;

public partial class PalveluPageEdit : ContentPage
{
	public PalveluPageEdit()
	{
		InitializeComponent();
	}

    async void PalaaPalveluPageClicked(object sender, EventArgs e)
    {
        // Use absolute routing for Shell routes (prefix with ///)
        await Shell.Current.GoToAsync("//PalveluPage");
    }
}