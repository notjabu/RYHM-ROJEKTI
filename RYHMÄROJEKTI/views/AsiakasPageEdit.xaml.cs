using System;
using System.Collections.Generic;
using System.Net.Http.Json;

namespace RYHMÄROJEKTI.views;

[QueryProperty(nameof(AsiakasId), "asiakasId")]
public partial class AsiakasPageEdit : ContentPage
{
<<<<<<< HEAD
=======
    private const string ConnectionString = "Server=127.0.0.1;Port=3307;Database=vn;User=root;Password=;SslMode=None;";
>>>>>>> b6bbdf4c78ccac69be220bbb6c26b2955fa82c37

    private int? _asiakasId;
    private string _origEtunimi = string.Empty;
    private string _origSukunimi = string.Empty;
    private string _origLahiosoite = string.Empty;
    private string _origPostinro = string.Empty;
    private string _origEmail = string.Empty;
    private string _origPuhelinnro = string.Empty;

    private readonly List<PostiDto> _postiItems = new();

    public string AsiakasId
    {
        set
        {
            if (int.TryParse(value, out var id))
                _asiakasId = id;
        }
    }

    public AsiakasPageEdit()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadPostiListAsync();

        if (_asiakasId.HasValue)
        {
            Title = "Muokkaa asiakasta";
            await LoadAsiakasAsync(_asiakasId.Value);
        }
        else
        {
            Title = "Lisää asiakas";
            EtunimiEntry.Text = string.Empty;
            SukunimiEntry.Text = string.Empty;
            LahiosoiteEntry.Text = string.Empty;
            PostiPicker.SelectedIndex = -1;
            EmailEntry.Text = string.Empty;
            PuhelinnroEntry.Text = string.Empty;
        }
    }

    // ── Posti picker ────────────────────────────────────────────────────
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

    private void OnPostiPickerChanged(object sender, EventArgs e) { }

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

    // ── Load existing asiakas ───────────────────────────────────────────
    private async Task LoadAsiakasAsync(int id)
    {
        try
        {
            var dto = await ApiClient.Http.GetFromJsonAsync<AsiakasDto>($"/api/asiakas/{id}");

            if (dto != null)
            {
                EtunimiEntry.Text = dto.Etunimi ?? string.Empty;
                SukunimiEntry.Text = dto.Sukunimi ?? string.Empty;
                LahiosoiteEntry.Text = dto.Lahiosoite ?? string.Empty;
                SelectPostiByPostinro(dto.Postinro ?? string.Empty);
                EmailEntry.Text = dto.Email ?? string.Empty;
                PuhelinnroEntry.Text = dto.Puhelinnro ?? string.Empty;

                _origEtunimi = dto.Etunimi ?? string.Empty;
                _origSukunimi = dto.Sukunimi ?? string.Empty;
                _origLahiosoite = dto.Lahiosoite ?? string.Empty;
                _origPostinro = dto.Postinro ?? string.Empty;
                _origEmail = dto.Email ?? string.Empty;
                _origPuhelinnro = dto.Puhelinnro ?? string.Empty;
            }
            else
            {
                await DisplayAlert("Virhe", "Asiakasta ei löytynyt.", "OK");
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    // ── Cancel ──────────────────────────────────────────────────────────
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _asiakasId = null;
        await Shell.Current.GoToAsync("..");
    }

    // ── Save ────────────────────────────────────────────────────────────
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var etunimi = EtunimiEntry.Text?.Trim() ?? string.Empty;
        var sukunimi = SukunimiEntry.Text?.Trim() ?? string.Empty;
        var lahiosoite = LahiosoiteEntry.Text?.Trim() ?? string.Empty;
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var puhelinnro = PuhelinnroEntry.Text?.Trim() ?? string.Empty;

        var selectedPosti = GetSelectedPosti();
        var postinro = selectedPosti?.Postinro ?? string.Empty;

        if (string.IsNullOrEmpty(etunimi) && string.IsNullOrEmpty(sukunimi))
        {
            await DisplayAlert("Virhe", "Anna vähintään etunimi tai sukunimi.", "OK");
            return;
        }

        var saveDto = new AsiakasSaveDto
        {
            Etunimi = etunimi,
            Sukunimi = sukunimi,
            Lahiosoite = lahiosoite,
            Postinro = postinro,
            Email = email,
            Puhelinnro = puhelinnro
        };

        // ── Editing ─────────────────────────────────────────────────────
        if (_asiakasId.HasValue)
        {
            if (etunimi == _origEtunimi && sukunimi == _origSukunimi && lahiosoite == _origLahiosoite
                && postinro == _origPostinro && email == _origEmail && puhelinnro == _origPuhelinnro)
            {
                _asiakasId = null;
                await Shell.Current.GoToAsync("..");
                return;
            }

            try
            {
                var resp = await ApiClient.Http.PutAsJsonAsync($"/api/asiakas/{_asiakasId.Value}", saveDto);
                resp.EnsureSuccessStatusCode();
                await DisplayAlert("Valmis", "Muutokset tallennettu.", "OK");
                _asiakasId = null;
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
            }
            return;
        }

        // ── Creating new ────────────────────────────────────────────────
        try
        {
            var resp = await ApiClient.Http.PostAsJsonAsync("/api/asiakas", saveDto);
            resp.EnsureSuccessStatusCode();
            await DisplayAlert("Valmis", "Asiakas tallennettu.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
        }
    }
}
