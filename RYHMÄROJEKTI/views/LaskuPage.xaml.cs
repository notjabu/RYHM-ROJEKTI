using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;

namespace RYHMÄROJEKTI.views;

public partial class LaskuPage : ContentPage
{
public LaskuPage()
{
    InitializeComponent();
    QuestPDF.Settings.License = LicenseType.Community;
    BindingContext = new LaskuPageViewModel();
}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is LaskuPageViewModel vm)
        {
            await vm.LataaLaskutAsync();
        }
    }

    // --- (LisaaLasku ja Muokkaa metodit säilyvät ennallaan) ---
    async void LisaaLasku_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("LaskuPageEdit");

    async void Muokkaa_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is not LaskuPageViewModel vm || vm.ValittuLasku == null)
        {
            await DisplayAlert("Huom", "Valitse muokattava lasku ensin.", "OK");
            return;
        }
        if (vm.ValittuLasku.Id.HasValue)
            await Shell.Current.GoToAsync($"LaskuPageEdit?laskuId={vm.ValittuLasku.Id.Value}");
    }

    // ── ViewModel ───────────────────────────────────────────────────────
    class LaskuPageViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<LaskuItem> Laskut { get; } = new();

        private LaskuItem _valittuLasku;
        public LaskuItem ValittuLasku
        {
            get => _valittuLasku;
            set { _valittuLasku = value; OnPropertyChanged(); }
        }

        public ICommand PoistaLaskuCommand { get; }
        // UUSI KOMENTO:
        public ICommand LuoPdfLaskuCommand { get; }

        public LaskuPageViewModel()
        {
            LuoPdfLaskuCommand = new Command(async () => await GeneroiPdfAsync());

            PoistaLaskuCommand = new Command(async () =>
            {
                if (ValittuLasku == null) return;
                bool ok = await Shell.Current.DisplayAlert("Vahvista", $"Poistetaanko lasku #{ValittuLasku.Id}?", "Kyllä", "Ei");
                if (!ok) return;

                try
                {
                    if (ValittuLasku.Id.HasValue)
                    {
                        var resp = await ApiClient.Http.DeleteAsync($"/api/lasku/{ValittuLasku.Id.Value}");
                        resp.EnsureSuccessStatusCode();
                    }
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert(
                        "Virhe", "Poisto epäonnistui: " + ex.Message, "OK");
                }

                Laskut.Remove(ValittuLasku);
                ValittuLasku = null;
            });
        }

        // UUSI METODI: PDF-generointi
        private async Task GeneroiPdfAsync()
        {
            if (ValittuLasku == null)
            {
                await Shell.Current.DisplayAlert("Virhe", "Valitse lasku listasta ensin!", "OK");
                return;
            }

            try
            {
                string fileName = $"Lasku_{ValittuLasku.Id}.pdf";
                string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                // Luodaan PDF-dokumentti
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(50);
                        page.Header().Text("VILLAGE NEWBIES - LASKU").FontSize(20).SemiBold().FontColor(QuestPDF.Helpers.Colors.Blue.Medium);

                        page.Content().Column(col =>
                        {
                            col.Spacing(10);
                            col.Item().Text($"Laskun numero: {ValittuLasku.Id}");
                            col.Item().Text($"Asiakas: {ValittuLasku.AsiakasNimi}");
                            col.Item().Text($"Mökki: {ValittuLasku.MokkiNimi}");
                            col.Item().LineHorizontal(1);
                            col.Item().Text($"Summa: {ValittuLasku.Summa:F2} €");
                            col.Item().Text($"ALV: {ValittuLasku.Alv}%");
                            col.Item().Text($"YHTEENSÄ: {ValittuLasku.Summa:F2} €").FontSize(16).Bold();
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Sivu ");
                            x.CurrentPageNumber();
                        });
                    });
                }).GeneratePdf(filePath);

                // Avataan PDF automaattisesti
                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(filePath)
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Virhe", "PDF:n luonti epäonnistui: " + ex.Message, "OK");
            }
        }

        public async Task LataaLaskutAsync()
        {
            try
            {
                Laskut.Clear();

                var list = await ApiClient.Http.GetFromJsonAsync<List<LaskuDto>>("/api/lasku");
                if (list == null) return;

                foreach (var dto in list)
                {
                    Laskut.Add(new LaskuItem
                    {
                        Id = dto.Id,
                        VarausId = dto.VarausId,
                        Summa = dto.Summa,
                        Alv = dto.Alv,
                        Maksettu = dto.Maksettu,
                        AsiakasNimi = dto.AsiakasNimi ?? string.Empty,
                        MokkiNimi = dto.MokkiNimi ?? string.Empty,
                        VarattuPvm = dto.VarattuPvm?.ToString("dd.MM.yyyy") ?? string.Empty,
                        VarattuAlkupvm = dto.VarattuAlkuPvm?.ToString("dd.MM.yyyy") ?? string.Empty,
                        VarattuLoppupvm = dto.VarattuLoppuPvm?.ToString("dd.MM.yyyy") ?? string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Latausvirhe",
                    $"Virhe koodissa tai kannassa: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine("LataaLaskutAsync error: " + ex);
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ── Model ───────────────────────────────────────────────────────────
    // Muista lisätä Laskutustapa malliin, jotta Picker toimii!
    class LaskuItem
    {
        public int? Id { get; set; }
        public int? VarausId { get; set; }
        public double Summa { get; set; }
        public double Alv { get; set; }
        public double Maksettu { get; set; }
        public string AsiakasNimi { get; set; } = string.Empty;
        public string MokkiNimi { get; set; } = string.Empty;
        public string VarattuPvm { get; set; } = string.Empty;
        public string VarattuAlkupvm { get; set; } = string.Empty;
        public string VarattuLoppupvm { get; set; } = string.Empty;
        public string LaskutusTapa { get; set; } // Lisätty tämä
        public double Maksamatta => Summa - Maksettu;
        public string Otsikko => $"Lasku #{Id} – {AsiakasNimi}";
        public string SummaText => $"{Summa:F2} € (ALV {Alv}%)";
        public string MaksettuText => $"{Maksettu:F2} €";
        public string MaksamattaText => $"{Maksamatta:F2} €";
    }
}