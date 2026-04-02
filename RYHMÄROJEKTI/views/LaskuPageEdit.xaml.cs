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

    private readonly List<VarausDto> _varaukset = new();
    private List<PalveluDto> _kaikkiPalvelut = new();
    private double _baseSumma; // nights*mökki + palvelut

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
        AdjustPicker.SelectedIndex = 0; // default "+"
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadPalvelutAsync();
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
            AdjustEntry.Text = string.Empty;
            AdjustPicker.SelectedIndex = 0;
        }
    }

    // ── Data loading ────────────────────────────────────────────────────

    private async Task LoadPalvelutAsync()
    {
        try
        {
            var list = await ApiClient.Http.GetFromJsonAsync<List<PalveluDto>>("/api/palvelu");
            _kaikkiPalvelut = list ?? new();
        }
        catch
        {
            _kaikkiPalvelut = new();
        }
    }

    private async Task LoadVarausListAsync()
    {
        try
        {
            _varaukset.Clear();
            VarausPicker.Items.Clear();

            var list = await ApiClient.Http.GetFromJsonAsync<List<VarausDto>>("/api/varaus");
            if (list == null) return;

            foreach (var v in list)
            {
                _varaukset.Add(v);

                var alkupvm = v.VarattuAlkuPvm?.ToString("dd.MM.yyyy") ?? "";
                var loppupvm = v.VarattuLoppuPvm?.ToString("dd.MM.yyyy") ?? "";
                VarausPicker.Items.Add(
                    $"#{v.VarausId} – {v.Etunimi} {v.Sukunimi} – {v.MokkiNimi} ({alkupvm}–{loppupvm})");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", "Varausten lataus epäonnistui: " + ex.Message, "OK");
        }
    }

    private void SelectVarausById(int varausId)
    {
        for (int i = 0; i < _varaukset.Count; i++)
        {
            if (_varaukset[i].VarausId == varausId)
            {
                VarausPicker.SelectedIndex = i;
                return;
            }
        }
    }

    private VarausDto? GetSelectedVaraus()
    {
        var idx = VarausPicker.SelectedIndex;
        if (idx >= 0 && idx < _varaukset.Count)
            return _varaukset[idx];
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
                AlvEntry.Text = dto.Alv.ToString("0.##");
                MaksettuEntry.Text = dto.Maksettu.ToString("0.##");

                _origVarausId = dto.VarausId;
                _origSumma = dto.Summa;
                _origAlv = dto.Alv;
                _origMaksettu = dto.Maksettu;

                // Calculate adjustment from saved summa vs base
                var diff = dto.Summa - _baseSumma;
                if (diff < 0)
                {
                    AdjustPicker.SelectedIndex = 1; // "−"
                    AdjustEntry.Text = Math.Abs(diff).ToString("0.##");
                }
                else if (diff > 0)
                {
                    AdjustPicker.SelectedIndex = 0; // "+"
                    AdjustEntry.Text = diff.ToString("0.##");
                }
                else
                {
                    AdjustPicker.SelectedIndex = 0;
                    AdjustEntry.Text = string.Empty;
                }
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

    // ── Cost calculation ────────────────────────────────────────────────

    private async void OnVarausPicker_Changed(object sender, EventArgs e)
    {
        await CalculateSummaAsync();
    }

    private void OnAdjust_Changed(object sender, EventArgs e)
    {
        RecalcSumma();
    }

    private async Task CalculateSummaAsync()
    {
        var varaus = GetSelectedVaraus();
        if (varaus == null)
        {
            _baseSumma = 0;
            SummaEntry.Text = string.Empty;
            ErittelyFrame.IsVisible = false;
            return;
        }

        // Fetch mökki price
        double mokkiHinta = 0;
        if (varaus.MokkiId.HasValue)
        {
            try
            {
                var mokki = await ApiClient.Http.GetFromJsonAsync<MokkiDto>($"/api/mokki/{varaus.MokkiId.Value}");
                mokkiHinta = mokki?.Hinta ?? 0;
            }
            catch { }
        }

        // Calculate nights
        int nights = 0;
        if (varaus.VarattuAlkuPvm.HasValue && varaus.VarattuLoppuPvm.HasValue)
            nights = (varaus.VarattuLoppuPvm.Value - varaus.VarattuAlkuPvm.Value).Days;
        if (nights < 0) nights = 0;

        double mokkiCost = nights * mokkiHinta;

        // Calculate palvelut cost
        double palvelutCost = 0;
        var palveluLines = new List<string>();
        if (varaus.Palvelut != null)
        {
            foreach (var vp in varaus.Palvelut)
            {
                var palvelu = _kaikkiPalvelut.FirstOrDefault(p => p.Id == vp.PalveluId);
                double hinta = palvelu?.Hinta ?? 0;
                double cost = hinta * vp.Lkm;
                palvelutCost += cost;
                if (vp.Lkm > 0)
                    palveluLines.Add($"  {vp.PalveluNimi}: {hinta:0.00} € × {vp.Lkm} = {cost:0.00} €");
            }
        }

        _baseSumma = mokkiCost + palvelutCost;

        // Build breakdown text
        var breakdown = $"Yöt: {nights} × {mokkiHinta:0.00} € = {mokkiCost:0.00} €";
        if (palveluLines.Count > 0)
            breakdown += "\nPalvelut:\n" + string.Join("\n", palveluLines);
        breakdown += $"\nYhteensä (perus): {_baseSumma:0.00} €";

        ErittelyLabel.Text = breakdown;
        ErittelyFrame.IsVisible = true;

        RecalcSumma();
    }

    private void RecalcSumma()
    {
        double adjust = 0;
        if (!string.IsNullOrWhiteSpace(AdjustEntry.Text))
        {
            if (!double.TryParse(AdjustEntry.Text.Trim(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out adjust))
            {
                double.TryParse(AdjustEntry.Text.Trim(), out adjust);
            }
        }

        bool isMinus = AdjustPicker.SelectedIndex == 1;
        double total = _baseSumma + (isMinus ? -adjust : adjust);
        if (total < 0) total = 0;

        SummaEntry.Text = total.ToString("0.00");
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

        var varausId = selectedVaraus.VarausId ?? 0;

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
}
