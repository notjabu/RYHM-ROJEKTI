using System;
using System.Collections.Generic;
using System.Net.Http.Json;

namespace RYHMÄROJEKTI.views;

[QueryProperty(nameof(LaskuId), "laskuId")]
public partial class LaskuPageEdit : ContentPage
{
private int? _laskuId;
    private int? _origVarausId;
    private double _origSumma;
    private double _origAlv;
    private double _origMaksettu;

    private readonly List<VarausItem> _varausItems = new();

    public string LaskuId
    {
        set
        {
            if (int.TryParse(value, out var id))
                _laskuId = id;
        }
    }

    public LaskuPageEdit()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadVarausListAsync();

        if (_laskuId.HasValue)
        {
            Title = "Muokkaa laskua";
            await LoadLaskuAsync(_laskuId.Value);
        }
        else
        {
            Title = "Lisää lasku";
            VarausPicker.SelectedIndex = -1;
            SummaEntry.Text = string.Empty;
            AlvEntry.Text = "24";
            MaksettuEntry.Text = "0";
        }
    }

    // ── Varaus picker ───────────────────────────────────────────────────
    private async Task LoadVarausListAsync()
    {
        try
        {
            _varausItems.Clear();
            VarausPicker.Items.Clear();

            var list = await ApiClient.Http.GetFromJsonAsync<List<VarausDto>>("/api/varaus");
            if (list == null) return;

            foreach (var v in list)
            {
                var alkupvm = v.VarattuAlkuPvm?.ToString("dd.MM.yyyy") ?? string.Empty;
                var loppupvm = v.VarattuLoppuPvm?.ToString("dd.MM.yyyy") ?? string.Empty;

                var item = new VarausItem
                {
                    VarausId = v.VarausId ?? 0,
                    Display = $"#{v.VarausId} – {v.Etunimi} {v.Sukunimi} – {v.MokkiNimi} ({alkupvm}–{loppupvm})"
                };

                _varausItems.Add(item);
                VarausPicker.Items.Add(item.Display);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", "Varausten lataus epäonnistui: " + ex.Message, "OK");
        }
    }

    private void SelectVarausById(int varausId)
    {
        for (int i = 0; i < _varausItems.Count; i++)
        {
            if (_varausItems[i].VarausId == varausId)
            {
                VarausPicker.SelectedIndex = i;
                return;
            }
        }
    }

    private VarausItem? GetSelectedVaraus()
    {
        var idx = VarausPicker.SelectedIndex;
        if (idx >= 0 && idx < _varausItems.Count)
            return _varausItems[idx];
        return null;
    }

    // ── Load existing lasku ─────────────────────────────────────────────
    private async Task LoadLaskuAsync(int id)
    {
        try
        {
            var dto = await ApiClient.Http.GetFromJsonAsync<LaskuDto>($"/api/lasku/{id}");

            if (dto != null)
            {
                SelectVarausById(dto.VarausId ?? 0);
                SummaEntry.Text = dto.Summa.ToString("0.##");
                AlvEntry.Text = dto.Alv.ToString("0.##");
                MaksettuEntry.Text = dto.Maksettu.ToString("0.##");

                _origVarausId = dto.VarausId;
                _origSumma = dto.Summa;
                _origAlv = dto.Alv;
                _origMaksettu = dto.Maksettu;
            }
            else
            {
                await DisplayAlert("Virhe", "Laskua ei löytynyt.", "OK");
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
        _laskuId = null;
        await Shell.Current.GoToAsync("..");
    }

    // ── Save ────────────────────────────────────────────────────────────
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var selectedVaraus = GetSelectedVaraus();
        if (selectedVaraus == null)
        {
            await DisplayAlert("Virhe", "Valitse varaus.", "OK");
            return;
        }

        var varausId = selectedVaraus.VarausId;

        if (!double.TryParse(SummaEntry.Text?.Trim(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var summa))
        {
            if (!double.TryParse(SummaEntry.Text?.Trim(), out summa))
                summa = 0;
        }

        if (!double.TryParse(AlvEntry.Text?.Trim(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var alv))
        {
            if (!double.TryParse(AlvEntry.Text?.Trim(), out alv))
                alv = 0;
        }

        if (!double.TryParse(MaksettuEntry.Text?.Trim(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var maksettu))
        {
            if (!double.TryParse(MaksettuEntry.Text?.Trim(), out maksettu))
                maksettu = 0;
        }

        var saveDto = new LaskuSaveDto
        {
            VarausId = varausId,
            Summa = summa,
            Alv = alv,
            Maksettu = maksettu
        };

        // ── Editing ─────────────────────────────────────────────────────
        if (_laskuId.HasValue)
        {
            if (varausId == _origVarausId && summa == _origSumma
                && alv == _origAlv && maksettu == _origMaksettu)
            {
                _laskuId = null;
                await Shell.Current.GoToAsync("..");
                return;
            }

            try
            {
                var resp = await ApiClient.Http.PutAsJsonAsync($"/api/lasku/{_laskuId.Value}", saveDto);
                resp.EnsureSuccessStatusCode();
                await DisplayAlert("Valmis", "Muutokset tallennettu.", "OK");
                _laskuId = null;
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
            var resp = await ApiClient.Http.PostAsJsonAsync("/api/lasku", saveDto);
            resp.EnsureSuccessStatusCode();
            await DisplayAlert("Valmis", "Lasku tallennettu.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
        }
    }

    private class VarausItem
    {
        public int VarausId { get; set; }
        public string Display { get; set; } = string.Empty;
    }
}
