namespace RYHMÄROJEKTI.views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Maui.Controls;

[QueryProperty(nameof(PalveluId), "palveluId")]
public partial class PalveluPageEdit : ContentPage
{
    private int? _palveluId;

    private readonly ObservableCollection<AreaItem> _areas = new();

    public string PalveluId
    {
        set
        {
            if (int.TryParse(value, out var id))
            {
                _palveluId = id;
                _ = LoadPalveluAsync(id);
            }
        }
    }

    public PalveluPageEdit()
    {
        InitializeComponent();
        SijaintiPicker.ItemsSource = _areas;
        SijaintiPicker.ItemDisplayBinding = new Binding("Nimi");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAreasAsync();
    }

    async void PalaaPalveluPageClicked(object sender, EventArgs e)  
    {
        await Shell.Current.GoToAsync("//PalveluPage");
    }

    private async Task LoadAreasAsync()
    {
        try
        {
            _areas.Clear();
            var list = await ApiClient.Http.GetFromJsonAsync<List<AlueDto>>("/api/alue");
            if (list == null) return;

            foreach (var a in list)
            {
                if (a.Id.HasValue && !string.IsNullOrWhiteSpace(a.Nimi))
                    _areas.Add(new AreaItem { Id = a.Id.Value, Nimi = a.Nimi });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("LoadAreasAsync error: " + ex);
        }
    }

    private async Task LoadPalveluAsync(int id)
    {
        try
        {
            await LoadAreasAsync();

            var dto = await ApiClient.Http.GetFromJsonAsync<PalveluDto>($"/api/palvelu/{id}");

            if (dto != null)
            {
                NimiEntry.Text = dto.Nimi ?? string.Empty;
                KuvausEditor.Text = dto.Kuvaus ?? string.Empty;

                // set price and VAT fields (use current culture for decimal separator)
                HintaEntry.Text = dto.Hinta.ToString("0.##", CultureInfo.CurrentCulture);
                AlvEntry.Text = dto.Alv.ToString("0.##", CultureInfo.CurrentCulture);

                if (dto.AlueId.HasValue)
                {
                    var match = _areas.FirstOrDefault(a => a.Id == dto.AlueId.Value);
                    if (match != null)
                        SijaintiPicker.SelectedItem = match;
                    else
                    {
                        var placeholder = new AreaItem { Id = dto.AlueId.Value, Nimi = $"Alue #{dto.AlueId.Value}" };
                        _areas.Add(placeholder);
                        SijaintiPicker.SelectedItem = placeholder;
                    }
                }
                else
                {
                    SijaintiPicker.SelectedItem = null;
                }
            }
            else
            {
                await DisplayAlert("Virhe", "Palvelua ei löytynyt.", "OK");
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var nimi = NimiEntry.Text?.Trim() ?? string.Empty;
        var selectedArea = SijaintiPicker.SelectedItem as AreaItem;
        int? alueId = selectedArea?.Id;
        var kuvaus = KuvausEditor.Text?.Trim() ?? string.Empty;

        // parse price and VAT using current culture
        double hinta = 0;
        double alv = 0;
        if (!string.IsNullOrWhiteSpace(HintaEntry.Text) &&
            !double.TryParse(HintaEntry.Text.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out hinta))
        {
            await DisplayAlert("Virhe", "Hinnan muoto ei ole kelvollinen.", "OK");
            return;
        }
        if (!string.IsNullOrWhiteSpace(AlvEntry.Text) &&
            !double.TryParse(AlvEntry.Text.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out alv))
        {
            await DisplayAlert("Virhe", "ALV:n muoto ei ole kelvollinen.", "OK");
            return;
        }

        if (string.IsNullOrEmpty(nimi))
        {
            await DisplayAlert("Virhe", "Nimi ei voi olla tyhjä.", "OK");
            return;
        }

        var saveDto = new PalveluSaveDto
        {
            AlueId = alueId,
            Nimi = nimi,
            Kuvaus = kuvaus,
            Hinta = hinta,
            Alv = alv
        };

        try
        {
            if (_palveluId.HasValue)
            {
                var resp = await ApiClient.Http.PutAsJsonAsync($"/api/palvelu/{_palveluId.Value}", saveDto);
                resp.EnsureSuccessStatusCode();
                await DisplayAlert("Valmis", "Muutokset tallennettu.", "OK");
            }
            else
            {
                var resp = await ApiClient.Http.PostAsJsonAsync("/api/palvelu", saveDto);
                resp.EnsureSuccessStatusCode();
                await DisplayAlert("Valmis", "Palvelu tallennettu.", "OK");
            }

            _palveluId = null;
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
        }
    }

    class AreaItem
    {
        public int Id { get; set; }
        public string Nimi { get; set; } = string.Empty;
        public override string ToString() => Nimi;
    }
}