using Microsoft.Maui.Storage;
using System;
using System.IO;
using System.Threading.Tasks;
using RYHMÄROJEKTI.Models;

namespace RYHMÄROJEKTI.views;

public partial class MokkiPageEdit : ContentPage
{
    private readonly Mokki _editing;
    private readonly TaskCompletionSource<Mokki?> _tcs = new();

    // External caller can await this task to get the result (or null if cancelled)
    public Task<Mokki?> Completion => _tcs.Task;

    public Mokki? Result { get; private set; }

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

            var stream = await result.OpenReadAsync();
            var destPath = Path.Combine(FileSystem.CacheDirectory, result.FileName);
            using (var dest = File.OpenWrite(destPath))
            {
                await stream.CopyToAsync(dest);
            }

            _editing.Kuva = destPath;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", $"Kuvan valinta epäonnistui: {ex.Message}", "OK");
        }
    }

    async void OnTallennaClicked(object sender, EventArgs e)
    {
        Result = _editing;
        _tcs.TrySetResult(_editing);
        await Navigation.PopAsync();
    }

    async void OnPeruutaClicked(object sender, EventArgs e)
    {
        Result = null;
        _tcs.TrySetResult(null);
        await Navigation.PopAsync();
    }
}