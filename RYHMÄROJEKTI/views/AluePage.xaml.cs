using System;
using Microsoft.Maui.Controls;

namespace RYHMÄROJEKTI.views;

public partial class AluePage : ContentPage
{
	public AluePage()
	{
		InitializeComponent();
	}

    async void LisaaAlue_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///AluePageEdit");
    }

    async void Muokkaa_Clicked(object sender, EventArgs e)
    {
        // Try to read ValittuAlue from the BindingContext (safe reflection)
        var valittu = BindingContext?.GetType().GetProperty("ValittuAlue")?.GetValue(BindingContext);
        if (valittu == null)
        {
            await DisplayAlert("Huom", "Valitse muokattava alue ensin.", "OK");
            return;
        }

        // Navigate to edit page (absolute route matching AppShell)
        await Shell.Current.GoToAsync("///AluePageEdit");
    }
}