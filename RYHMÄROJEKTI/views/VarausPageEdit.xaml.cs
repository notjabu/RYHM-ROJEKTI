using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;
using System.Linq;

namespace RYHMÄROJEKTI.views;

[QueryProperty(nameof(VarausId), "varausId")]
public partial class VarausPageEdit : ContentPage
{
    private int? _varausId;
    private readonly List<AsiakasDto> _asiakkaat = new();
    private readonly List<MokkiDto> _mokit = new();
    private List<PalveluDto> _kaikkiPalvelut = new();
    private readonly ObservableCollection<PalveluSelection> _palvelut = new();
    private List<MokkiVarausDto> _mokkiVaraukset = new();

    public string VarausId
    {
        set
        {
            if (int.TryParse(value, out var id))
                _varausId = id;
        }
    }

    public VarausPageEdit()
    {
        InitializeComponent();

        AlkuPvmPicker.MinimumDate = DateTime.Today;
        AlkuPvmPicker.Date = DateTime.Today;
        LoppuPvmPicker.MinimumDate = DateTime.Today.AddDays(1);
        LoppuPvmPicker.Date = DateTime.Today.AddDays(1);

        PalvelutListView.ItemsSource = _palvelut;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsiakkaatAsync();
        await LoadKaikkiPalvelutAsync();
        await LoadMokitAsync();

        if (_varausId.HasValue)
        {
            Title = "Muokkaa varausta";
            await LoadVarausAsync(_varausId.Value);
        }
        else
        {
            Title = "Lisää varaus";
        }
    }

    // ── Data loading ────────────────────────────────────────────────────

    private async Task LoadAsiakkaatAsync()
    {
        try
        {
            _asiakkaat.Clear();
            AsiakasPicker.Items.Clear();

            var list = await ApiClient.Http.GetFromJsonAsync<List<AsiakasDto>>("/api/asiakas");
            if (list == null) return;

            foreach (var a in list)
            {
                _asiakkaat.Add(a);
                AsiakasPicker.Items.Add($"{a.Etunimi} {a.Sukunimi}".Trim());
            }

            // Jos tultu AsiakasPageEdit:ista, valitsee viimeisimman lisatyn asiakkaan
            if (_asiakkaat.Count > 0)
                AsiakasPicker.SelectedIndex = _asiakkaat.Count - 1;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", "Asiakkaiden lataus epäonnistui: " + ex.Message, "OK");
        }
    }

    private async Task LoadMokitAsync()
    {
        try
        {
            _mokit.Clear();
            MokkiPicker.Items.Clear();

            var list = await ApiClient.Http.GetFromJsonAsync<List<MokkiDto>>("/api/mokki");
            if (list == null) return;

            foreach (var m in list)
            {
                _mokit.Add(m);
                MokkiPicker.Items.Add(m.Mokkinimi);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", "Mökkien lataus epäonnistui: " + ex.Message, "OK");
        }
    }

    private async Task LoadKaikkiPalvelutAsync()
    {
        try
        {
            var list = await ApiClient.Http.GetFromJsonAsync<List<PalveluDto>>("/api/palvelu");
            _kaikkiPalvelut = list ?? new();
        }
        catch (Exception ex)
        {
            _kaikkiPalvelut = new();
            await DisplayAlert("Virhe", "Palveluiden lataus epäonnistui: " + ex.Message, "OK");
        }
    }

    private void FilterPalvelutByAlue(int? alueId)
    {
        _palvelut.Clear();

        var filtered = alueId.HasValue
            ? _kaikkiPalvelut.Where(p => p.AlueId == alueId.Value)
            : _kaikkiPalvelut;

        foreach (var p in filtered)
        {
            _palvelut.Add(new PalveluSelection
            {
                PalveluId = p.Id ?? 0,
                Nimi = p.Nimi,
                Hinta = p.Hinta
            });
        }
    }

    private async Task LoadVarausAsync(int id)
    {
        try
        {
            var dto = await ApiClient.Http.GetFromJsonAsync<VarausDto>($"/api/varaus/{id}");
            if (dto == null)
            {
                await DisplayAlert("Virhe", "Varausta ei löytynyt.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            // Valitse asiakas
            for (int i = 0; i < _asiakkaat.Count; i++)
            {
                if (_asiakkaat[i].Id == dto.AsiakasId)
                {
                    AsiakasPicker.SelectedIndex = i;
                    break;
                }
            }

            // Valitse mokki
            for (int i = 0; i < _mokit.Count; i++)
            {
                if (_mokit[i].Id == dto.MokkiId)
                {
                    MokkiPicker.SelectedIndex = i;
                    break;
                }
            }

            // Preselect palvelut jotka ovat tässä varauksessa.
            // If some services are not in the current filtered list, add them so they remain visible.
            if (dto.Palvelut != null)
            {
                foreach (var vp in dto.Palvelut)
                {
                    var match = _palvelut.FirstOrDefault(p => p.PalveluId == vp.PalveluId);
                    if (match != null)
                    {
                        match.IsSelected = true;
                        match.Lkm = vp.Lkm;
                    }
                    else
                    {
                        // Find service in all services and add it so the user can see/edit it
                        var pDto = _kaikkiPalvelut.FirstOrDefault(x => x.Id == vp.PalveluId);
                        if (pDto != null)
                        {
                            var added = new PalveluSelection
                            {
                                PalveluId = pDto.Id ?? 0,
                                Nimi = pDto.Nimi,
                                Hinta = pDto.Hinta,
                                IsSelected = true,
                                Lkm = vp.Lkm
                            };
                            _palvelut.Add(added);
                        }
                    }
                }
            }

            // Syötä päivät
            if (dto.VarattuAlkuPvm.HasValue)
            {
                AlkuPvmPicker.MinimumDate = dto.VarattuAlkuPvm.Value < DateTime.Today
                    ? dto.VarattuAlkuPvm.Value : DateTime.Today;
                AlkuPvmPicker.Date = dto.VarattuAlkuPvm.Value;
            }
            if (dto.VarattuLoppuPvm.HasValue)
            {
                LoppuPvmPicker.MinimumDate = AlkuPvmPicker.Date.AddDays(1);
                LoppuPvmPicker.Date = dto.VarattuLoppuPvm.Value;
            }

            // Syötä vahvistus
            VahvistaCheckBox.IsChecked = dto.VahvistusPvm.HasValue;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    // ── Events ──────────────────────────────────────────────────────────

    private void OnAsiakasPicker_Changed(object sender, EventArgs e) { }

    private async void OnMokkiPicker_Changed(object sender, EventArgs e)
    {
        var idx = MokkiPicker.SelectedIndex;
        if (idx >= 0 && idx < _mokit.Count)
        {
            var alueId = _mokit[idx].AlueId;
            // If the checkbox is checked, show all services (include external). Otherwise show only this area.
            if (UlkopalvelutCheckBox.IsChecked)
                FilterPalvelutByAlue(null);
            else
                FilterPalvelutByAlue(alueId);

            await LoadMokkiVarauksetAsync(_mokit[idx].Id ?? 0);
        }
        else
        {
            _palvelut.Clear();
            _mokkiVaraukset.Clear();
            VarauksetFrame.IsVisible = false;
        }
    }

    private async Task LoadMokkiVarauksetAsync(int mokkiId)
    {
        try
        {
            var list = await ApiClient.Http.GetFromJsonAsync<List<MokkiVarausDto>>($"/api/varaus/mokki/{mokkiId}");
            _mokkiVaraukset = list ?? new();

            if (_varausId.HasValue)
                _mokkiVaraukset.RemoveAll(v => v.VarausId == _varausId.Value);

            if (_mokkiVaraukset.Count > 0)
            {
                var lines = _mokkiVaraukset.Select(v =>
                    $"\u2022 {v.VarattuAlkuPvm:dd.MM.yyyy} \u2013 {v.VarattuLoppuPvm:dd.MM.yyyy}");
                VarauksetLabel.Text = string.Join("\n", lines);
                VarauksetFrame.IsVisible = true;
            }
            else
            {
                VarauksetFrame.IsVisible = false;
            }
        }
        catch
        {
            _mokkiVaraukset = new();
            VarauksetFrame.IsVisible = false;
        }
    }

    private async void OnLisaaAsiakas_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AsiakasPageEdit");
    }

    private void OnAlkuPvm_Changed(object sender, DateChangedEventArgs e)
    {
        var minLoppu = e.NewDate.AddDays(1);
        LoppuPvmPicker.MinimumDate = minLoppu;
        if (LoppuPvmPicker.Date < minLoppu)
            LoppuPvmPicker.Date = minLoppu;
    }

    // Checkbox: include external-area services
    private void OnUlkopalvelutToggled(object sender, CheckedChangedEventArgs e)
    {
        var idx = MokkiPicker.SelectedIndex;
        if (idx >= 0 && idx < _mokit.Count)
        {
            var alueId = _mokit[idx].AlueId;
            FilterPalvelutByAlue(e.Value ? null : alueId);
        }
        else
        {
            // No cottage selected: if checked, show all; if not, clear list
            if (e.Value)
                FilterPalvelutByAlue(null);
            else
                _palvelut.Clear();
        }
    }

    // ── Cancel ──────────────────────────────────────────────────────────

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _varausId = null;
        await Shell.Current.GoToAsync("..");
    }

    // ── Save ────────────────────────────────────────────────────────────

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validioi asiakas
        var asiakasIdx = AsiakasPicker.SelectedIndex;
        if (asiakasIdx < 0 || asiakasIdx >= _asiakkaat.Count)
        {
            await DisplayAlert("Virhe", "Valitse asiakas.", "OK");
            return;
        }

        // Validioi mökki
        var mokkiIdx = MokkiPicker.SelectedIndex;
        if (mokkiIdx < 0 || mokkiIdx >= _mokit.Count)
        {
            await DisplayAlert("Virhe", "Valitse mökki.", "OK");
            return;
        }

        var alkuPvm = AlkuPvmPicker.Date;
        var loppuPvm = LoppuPvmPicker.Date;

        if (alkuPvm < DateTime.Today)
        {
            await DisplayAlert("Virhe", "Alkupäivämäärä ei voi olla menneisyydessä.", "OK");
            return;
        }

        if (loppuPvm <= alkuPvm)
        {
            await DisplayAlert("Virhe", "Loppupäivämäärän täytyy olla vähintään päivä alkupäivämäärän jälkeen.", "OK");
            return;
        }

        // Tarkistaa päällekkäis varaukset
        var overlap = _mokkiVaraukset.FirstOrDefault(v =>
            v.VarattuAlkuPvm.HasValue && v.VarattuLoppuPvm.HasValue &&
            alkuPvm < v.VarattuLoppuPvm.Value && loppuPvm > v.VarattuAlkuPvm.Value);
        if (overlap != null)
        {
            await DisplayAlert("Virhe",
                $"Mökki on jo varattu aikavälillä {overlap.VarattuAlkuPvm:dd.MM.yyyy} \u2013 {overlap.VarattuLoppuPvm:dd.MM.yyyy}. Valitse toiset päivämäärät.",
                "OK");
            return;
        }

        var asiakas = _asiakkaat[asiakasIdx];
        var mokki = _mokit[mokkiIdx];

        var palvelut = new List<VarausPalveluSaveDto>();
        foreach (var p in _palvelut)
        {
            if (p.IsSelected && p.Lkm > 0)
            {
                palvelut.Add(new VarausPalveluSaveDto
                {
                    PalveluId = p.PalveluId,
                    Lkm = p.Lkm
                });
            }
        }

        var dto = new VarausSaveDto
        {
            AsiakasId = asiakas.Id ?? 0,
            MokkiId = mokki.Id ?? 0,
            VahvistusPvm = VahvistaCheckBox.IsChecked ? DateTime.Now : null,
            VarattuAlkuPvm = alkuPvm,
            VarattuLoppuPvm = loppuPvm,
            Palvelut = palvelut
        };

        try
        {
            if (_varausId.HasValue)
            {
                var resp = await ApiClient.Http.PutAsJsonAsync($"/api/varaus/{_varausId.Value}", dto);
                resp.EnsureSuccessStatusCode();
                await DisplayAlert("Valmis", "Varaus päivitetty.", "OK");
            }
            else
            {
                var resp = await ApiClient.Http.PostAsJsonAsync("/api/varaus", dto);
                resp.EnsureSuccessStatusCode();
                await DisplayAlert("Valmis", "Varaus tallennettu.", "OK");
            }
            _varausId = null;
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
        }
    }

    // ── Palvelu selection helper ────────────────────────────────────────

    class PalveluSelection : INotifyPropertyChanged
    {
        public int PalveluId { get; set; }
        public string Nimi { get; set; } = "";
        public double Hinta { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        private int _lkm = 1;
        public int Lkm
        {
            get => _lkm;
            set { _lkm = value; OnPropertyChanged(); }
        }

        public string DisplayText => $"{Nimi} ({Hinta:F2} €)";

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
