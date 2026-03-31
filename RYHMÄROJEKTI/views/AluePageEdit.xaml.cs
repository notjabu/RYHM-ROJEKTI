using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;

namespace RYHMÄROJEKTI.views;

[QueryProperty(nameof(AlueId), "alueId")]
public partial class AluePageEdit : ContentPage
{
private int? _alueId;
    private string _origNimi = string.Empty;
    private string _origPostinumero = string.Empty;
    private string _origKuvaus = string.Empty;

    private readonly List<PostiDto> _postiItems = new();

    public string AlueId
    {
        set
        {
            if (int.TryParse(value, out var id))
                _alueId = id;
        }
    }

    public AluePageEdit()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadPostiListAsync();

        if (_alueId.HasValue)
        {
            Title = "Muokkaa aluetta";
            await LoadAlueAsync(_alueId.Value);
        }
        else
        {
            Title = "Lisää alue";
            NimiEntry.Text = string.Empty;
            PostiPicker.SelectedIndex = -1;
            KuvausEditor.Text = string.Empty;
        }
    }

    private async Task LoadPostiListAsync()
    {
        try
        {
            _postiItems.Clear();
            PostiPicker.Items.Clear();

            var list = await ApiClient.Http.GetFromJsonAsync<List<PostiDto>>("/api/posti");
            if (list == null) return;

            foreach (var p in list)
            {
                _postiItems.Add(p);
                PostiPicker.Items.Add($"{p.Postinro} – {p.Toimipaikka}");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", "Postinumeroiden lataus epäonnistui: " + ex.Message, "OK");
        }
    }

    private void OnPostiPickerChanged(object sender, EventArgs e)
    {
    }

    private async void OnLisaaPostiClicked(object sender, EventArgs e)
    {
        var postinro = await DisplayPromptAsync("Uusi postinumero", "Syötä postinumero:", keyboard: Keyboard.Numeric);
        if (string.IsNullOrWhiteSpace(postinro)) return;

        var toimipaikka = await DisplayPromptAsync("Uusi toimipaikka", "Syötä postitoimipaikka:");
        if (string.IsNullOrWhiteSpace(toimipaikka)) return;

        postinro = postinro.Trim();
        toimipaikka = toimipaikka.Trim();

        try
        {
            var resp = await ApiClient.Http.PostAsJsonAsync("/api/posti", new PostiDto { Postinro = postinro, Toimipaikka = toimipaikka });
            resp.EnsureSuccessStatusCode();

            await LoadPostiListAsync();
            SelectPostiByPostinro(postinro);

            await DisplayAlert("Valmis", "Postinumero lisätty.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
        }
    }

    private void SelectPostiByPostinro(string postinro)
    {
        for (int i = 0; i < _postiItems.Count; i++)
        {
            if (_postiItems[i].Postinro == postinro)
            {
                PostiPicker.SelectedIndex = i;
                return;
            }
        }
    }

    private PostiDto? GetSelectedPosti()
    {
        var idx = PostiPicker.SelectedIndex;
        if (idx >= 0 && idx < _postiItems.Count)
            return _postiItems[idx];
        return null;
    }

    private async Task LoadAlueAsync(int id)
    {
        try
        {
            var dto = await ApiClient.Http.GetFromJsonAsync<AlueDto>($"/api/alue/{id}");

            if (dto != null)
            {
                NimiEntry.Text = dto.Nimi ?? string.Empty;
                KuvausEditor.Text = dto.Kuvaus ?? string.Empty;
                SelectPostiByPostinro(dto.Postinumero ?? string.Empty);

                _origNimi = dto.Nimi ?? string.Empty;
                _origPostinumero = dto.Postinumero ?? string.Empty;
                _origKuvaus = dto.Kuvaus ?? string.Empty;
            }
            else
            {
                await DisplayAlert("Virhe", "Aluetta ei löytynyt.", "OK");
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _alueId = null;
        await Shell.Current.GoToAsync("..");
    }

    private async void OnSelectImageClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Valitse kuva", FileTypes = FilePickerFileType.Images });
            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", ex.Message, "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var nimi = NimiEntry.Text?.Trim() ?? string.Empty;
        var kuvaus = KuvausEditor.Text?.Trim() ?? string.Empty;

        var selectedPosti = GetSelectedPosti();
        var postinumero = selectedPosti?.Postinro ?? string.Empty;
        var postitoimipaikka = selectedPosti?.Toimipaikka ?? string.Empty;

        if (string.IsNullOrEmpty(nimi))
        {
            await DisplayAlert("Virhe", "Anna nimi.", "OK");
            return;
        }

        var saveDto = new AlueSaveDto { Nimi = nimi, Sijainti = postitoimipaikka, Kuvaus = kuvaus };

        // Editing existing record
        if (_alueId.HasValue)
        {
            if (nimi == _origNimi && postinumero == _origPostinumero && kuvaus == _origKuvaus)
            {
                _alueId = null;
                await Shell.Current.GoToAsync("..");
                return;
            }

            try
            {
                var resp = await ApiClient.Http.PutAsJsonAsync($"/api/alue/{_alueId.Value}", saveDto);
                resp.EnsureSuccessStatusCode();
                await DisplayAlert("Valmis", "Muutokset tallennettu.", "OK");
                _alueId = null;
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
            }
            return;
        }

        // Creating new record
        try
        {
            var resp = await ApiClient.Http.PostAsJsonAsync("/api/alue", saveDto);
            resp.EnsureSuccessStatusCode();
            await DisplayAlert("Valmis", "Alue tallennettu.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
        }
    }
}
