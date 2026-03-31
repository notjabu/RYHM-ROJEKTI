using System;
using System.Collections.Generic;
using System.Net.Http.Json;

namespace RYHMÄROJEKTI.views;

[QueryProperty(nameof(MokkiId), "mokkiId")]
public partial class MokkiPageEdit : ContentPage
{

    private int? _mokkiId;
    private string _origNimi = string.Empty;
    private string _origKatuosoite = string.Empty;
    private string _origPostinro = string.Empty;
    private double _origHinta;
    private int _origHenkilomaara;
    private string _origVarustelu = string.Empty;
    private string _origKuvaus = string.Empty;

    private readonly List<PostiDto> _postiItems = new();

    public string MokkiId
    {
        set
        {
            if (int.TryParse(value, out var id))
                _mokkiId = id;
        }
    }

    public MokkiPageEdit()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadPostiListAsync();

        if (_mokkiId.HasValue)
        {
            Title = "Muokkaa mökkiä";
            await LoadMokkiAsync(_mokkiId.Value);
        }
        else
        {
            Title = "Lisää mökki";
            NimiEntry.Text = string.Empty;
            KatuosoiteEntry.Text = string.Empty;
            PostiPicker.SelectedIndex = -1;
            HintaEntry.Text = string.Empty;
            HenkilomaaraEntry.Text = string.Empty;
            VarusteluEditor.Text = string.Empty;
            KuvausEditor.Text = string.Empty;
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

    // ── Load existing mokki ─────────────────────────────────────────────
    private async Task LoadMokkiAsync(int id)
    {
        try
        {
            var dto = await ApiClient.Http.GetFromJsonAsync<MokkiDto>($"/api/mokki/{id}");

            if (dto != null)
            {
                NimiEntry.Text = dto.Mokkinimi ?? string.Empty;
                KatuosoiteEntry.Text = dto.Katuosoite ?? string.Empty;
                SelectPostiByPostinro(dto.Postinro ?? string.Empty);
                HintaEntry.Text = dto.Hinta.ToString("0.##");
                HenkilomaaraEntry.Text = dto.Henkilomaara.ToString();
                VarusteluEditor.Text = dto.Varustelu ?? string.Empty;
                KuvausEditor.Text = dto.Kuvaus ?? string.Empty;

                _origNimi = dto.Mokkinimi ?? string.Empty;
                _origKatuosoite = dto.Katuosoite ?? string.Empty;
                _origPostinro = dto.Postinro ?? string.Empty;
                _origHinta = dto.Hinta;
                _origHenkilomaara = dto.Henkilomaara;
                _origVarustelu = dto.Varustelu ?? string.Empty;
                _origKuvaus = dto.Kuvaus ?? string.Empty;
            }
            else
            {
                await DisplayAlert("Virhe", "Mökkiä ei löytynyt.", "OK");
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
        _mokkiId = null;
        await Shell.Current.GoToAsync("..");
    }

    // ── Save ────────────────────────────────────────────────────────────
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var nimi = NimiEntry.Text?.Trim() ?? string.Empty;
        var katuosoite = KatuosoiteEntry.Text?.Trim() ?? string.Empty;
        var kuvaus = KuvausEditor.Text?.Trim() ?? string.Empty;
        var varustelu = VarusteluEditor.Text?.Trim() ?? string.Empty;

        var selectedPosti = GetSelectedPosti();
        var postinro = selectedPosti?.Postinro ?? string.Empty;

        if (!double.TryParse(HintaEntry.Text?.Trim(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var hinta))
        {
            if (!double.TryParse(HintaEntry.Text?.Trim(), out hinta))
                hinta = 0;
        }

        if (!int.TryParse(HenkilomaaraEntry.Text?.Trim(), out var henkilomaara))
            henkilomaara = 0;

        if (string.IsNullOrEmpty(nimi))
        {
            await DisplayAlert("Virhe", "Anna nimi.", "OK");
            return;
        }

        var saveDto = new MokkiSaveDto
        {
            Mokkinimi = nimi,
            Katuosoite = katuosoite,
            Postinro = postinro,
            Hinta = hinta,
            Henkilomaara = henkilomaara,
            Varustelu = varustelu,
            Kuvaus = kuvaus
        };

        // ── Editing ─────────────────────────────────────────────────────
        if (_mokkiId.HasValue)
        {
            if (nimi == _origNimi && katuosoite == _origKatuosoite && postinro == _origPostinro
                && hinta == _origHinta && henkilomaara == _origHenkilomaara
                && varustelu == _origVarustelu && kuvaus == _origKuvaus)
            {
                _mokkiId = null;
                await Shell.Current.GoToAsync("..");
                return;
            }

            try
            {
                var resp = await ApiClient.Http.PutAsJsonAsync($"/api/mokki/{_mokkiId.Value}", saveDto);
                resp.EnsureSuccessStatusCode();
                await DisplayAlert("Valmis", "Muutokset tallennettu.", "OK");
                _mokkiId = null;
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
            var resp = await ApiClient.Http.PostAsJsonAsync("/api/mokki", saveDto);
            resp.EnsureSuccessStatusCode();
            await DisplayAlert("Valmis", "Mökki tallennettu.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
        }
    }
}
