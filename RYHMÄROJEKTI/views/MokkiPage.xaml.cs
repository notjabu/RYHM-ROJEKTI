using RYHMÄROJEKTI.views;
namespace RYHMÄROJEKTI.views;

public partial class MokkiPage : ContentPage
{
	public MokkiPage()
	{
		InitializeComponent();
    }

    async void LisaaMokki_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///MokkiPageEdit");
    }
 
    async void Muokkaa_Clicked(object sender, EventArgs e)
    {
        // Try to read ValittuAlue from the BindingContext (safe reflection)
        var valittu = BindingContext?.GetType().GetProperty("ValittuMokki")?.GetValue(BindingContext);
        if (valittu == null)
        {
            await DisplayAlert("Huom", "Valitse muokattava mökki ensin.", "OK");
            return;
        }

        // Navigate to edit page (absolute route matching AppShell)
        await Shell.Current.GoToAsync("///MokkiPageEdit");
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Haetaan ViewModel ja käsketään sitä lataamaan tiedot
        if (BindingContext is ViewModels.MokkiViewModel vm)
        {
            await vm.LataaMokit();
        }
    }
}