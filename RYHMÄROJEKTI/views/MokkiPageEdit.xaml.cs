using RYHMÄROJEKTI.Models;
using Microsoft.Maui.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RYHMÄROJEKTI.views;

public partial class MokkiPageEdit : ContentPage
{
    private readonly Mokki _editing;
    public Mokki Result { get; private set; }

    public MokkiPageEdit(Mokki mokki)
    {
        InitializeComponent();
        _editing = mokki;
        BindingContext = _editing;
    }

    async void OnPickImageClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Valitse mökille kuva"
            });

            if (result == null)
                return;

            // Kopioidaan valittu kuva sovelluksen välimuistiin ja asetetaan polku propertyyn
            var stream = await result.OpenReadAsync();
            var destPath = Path.Combine(FileSystem.CacheDirectory, result.FileName);
            using (var dest = File.OpenWrite(destPath))
            {
                await stream.CopyToAsync(dest);
            }

            _editing.Kuva = destPath; // MAUI Image osaa käyttää tiedostopolkuja
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", $"Kuvan valinta epäonnistui: {ex.Message}", "OK");
        }
    }

    async void OnSaveClicked(object sender, EventArgs e)
    {
        // Jos luotiin uusi olio, asetetaan Result; muokkauksissa kutsuja hyödyntää olion muutoksia suoraan
        Result = _editing;
        await Navigation.PopAsync();
    }

    async void OnCancelClicked(object sender, EventArgs e)
    {
        Result = null;
        await Navigation.PopAsync();
    }
}