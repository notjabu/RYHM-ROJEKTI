using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;

namespace RYHMÄROJEKTI;

public partial class MainPage : ContentPage
{
    private List<VarausDto> _varaukset = new();
    private bool _customRange;
    private List<(int? id, string nimi)> _alueet = new();
    private int? _selectedAlueId = null;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadVarauksetAsync();
        await LoadAlueetAsync();
        ShowNykytilanne();
    }

    private async Task LoadVarauksetAsync()
    {
        try
        {
            var list = await ApiClient.Http.GetFromJsonAsync<List<VarausDto>>("/api/varaus");
            _varaukset = list ?? new();
        }
        catch (Exception ex)
        {
            _varaukset = new();
            await DisplayAlert("Virhe", "Varausten lataus epäonnistui: " + ex.Message, "OK");
        }
    }

    private async Task LoadAlueetAsync()
    {
        try
        {
            var list = await ApiClient.Http.GetFromJsonAsync<List<AlueDto>>("/api/alue");
            if (list != null)
            {
                _alueet = new List<(int?, string)> { (null, "Kaikki alueet") };
                foreach (var alue in list.OrderBy(a => a.Nimi))
                {
                    _alueet.Add((alue.Id, alue.Nimi));
                }

                // Update picker items - access control here after InitializeComponent
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var picker = this.FindByName<Picker>("AluePickerSelector");
                    if (picker != null)
                    {
                        picker.ItemsSource = _alueet.Select(a => a.nimi).ToList();
                        picker.SelectedIndex = 0;
                    }
                });

                _selectedAlueId = null;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", "Alueiden lataus epäonnistui: " + ex.Message, "OK");
        }
    }

    private void OnDateSelected(object sender, DateChangedEventArgs e)
    {
        // Only informational; actual filtering happens on "Hae" click.
    }

    private void OnAlueSelected(object sender, EventArgs e)
    {
        var picker = sender as Picker;
        if (picker != null && picker.SelectedIndex >= 0 && picker.SelectedIndex < _alueet.Count)
        {
            _selectedAlueId = _alueet[picker.SelectedIndex].id;
            ShowNykytilanne();
        }
    }

    private async void OnHaeClicked(object sender, EventArgs e)
    {
        var alku = AlkuDatePicker.Date;
        var loppu = LoppuDatePicker.Date;

        if (alku > loppu)
        {
            await DisplayAlert("Virhe", "Alkupäivämäärä ei voi olla loppupäivämäärän jälkeen.", "OK");
            return;
        }

        _customRange = true;
        ModeLabel.Text = $"Aikaväli: {alku:dd.MM.yyyy} – {loppu:dd.MM.yyyy}";
        FilterAndDisplay(alku, loppu);
    }

    private void OnNykytilanne_Clicked(object sender, EventArgs e)
    {
        ShowNykytilanne();
    }

    private void ShowNykytilanne()
    {
        _customRange = false;
        var today = DateTime.Today;
        ModeLabel.Text = "Näytetään tämänhetkinen tilanne";
        FilterAndDisplay(today, today);
    }

    private void FilterAndDisplay(DateTime alku, DateTime loppu)
    {
        // A reservation overlaps the period [alku, loppu] when
        // varattu_alkupvm <= loppu AND varattu_loppupvm >= alku
        var matching = _varaukset.Where(v =>
            v.VarattuAlkuPvm.HasValue && v.VarattuLoppuPvm.HasValue &&
            v.VarattuAlkuPvm.Value.Date <= loppu &&
            v.VarattuLoppuPvm.Value.Date >= alku).ToList();

        // Filter by selected area if not "Kaikki alueet"
        if (_selectedAlueId.HasValue)
        {
            matching = matching.Where(v => v.AlueId == _selectedAlueId).ToList();
        }

        // -- Mökit --
        var mokkiRows = matching
            .Select(v => new VarattuMokkiRow
            {
                Mokkinimi = v.MokkiNimi,
                Ajanjakso = $"{v.VarattuAlkuPvm:dd.MM.yyyy} – {v.VarattuLoppuPvm:dd.MM.yyyy}",
                Asiakas = $"{v.Etunimi} {v.Sukunimi}".Trim()
            })
            .OrderBy(r => r.Mokkinimi)
            .ToList();

        if (mokkiRows.Count > 0)
        {
            MokitCollection.ItemsSource = mokkiRows;
            MokitCollection.IsVisible = true;
            MokitEmptyLabel.IsVisible = false;
        }
        else
        {
            MokitCollection.IsVisible = false;
            MokitEmptyLabel.IsVisible = true;
        }

        // -- Palvelut --
        var palveluRows = matching
            .Where(v => v.Palvelut != null)
            .SelectMany(v => v.Palvelut.Select(p => new
            {
                p.PalveluNimi,
                p.Lkm,
                Mokki = v.MokkiNimi,
                Asiakas = $"{v.Etunimi} {v.Sukunimi}".Trim(),
                Alku = v.VarattuAlkuPvm,
                Loppu = v.VarattuLoppuPvm
            }))
            .Select(x => new VarattuPalveluRow
            {
                PalveluNimi = x.PalveluNimi,
                Tiedot = $"{x.Lkm} kpl – {x.Mokki} ({x.Asiakas}), {x.Alku:dd.MM.yyyy} – {x.Loppu:dd.MM.yyyy}"
            })
            .OrderBy(r => r.PalveluNimi)
            .ToList();

        if (palveluRows.Count > 0)
        {
            PalvelutCollection.ItemsSource = palveluRows;
            PalvelutCollection.IsVisible = true;
            PalvelutEmptyLabel.IsVisible = false;
        }
        else
        {
            PalvelutCollection.IsVisible = false;
            PalvelutEmptyLabel.IsVisible = true;
        }
    }

    // Display models
    class VarattuMokkiRow
    {
        public string Mokkinimi { get; set; } = "";
        public string Ajanjakso { get; set; } = "";
        public string Asiakas { get; set; } = "";
    }

    class VarattuPalveluRow
    {
        public string PalveluNimi { get; set; } = "";
        public string Tiedot { get; set; } = "";
    }
}