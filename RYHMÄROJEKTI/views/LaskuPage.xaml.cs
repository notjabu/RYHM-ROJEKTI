using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using MySqlConnector;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;

namespace RYHMÄROJEKTI.views;

public partial class LaskuPage : ContentPage
{
    private const string ConnectionString = "Server=127.0.0.1;Port=3307;Database=vn;User=root;Password=;SslMode=None;";

    public LaskuPage()
    {
        InitializeComponent();
        // Asetetaan QuestPDF-lisenssi (ilmainen yhteisölisenssi)
        QuestPDF.Settings.License = LicenseType.Community;
        BindingContext = new LaskuPageViewModel(ConnectionString);
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
        private readonly string _connString;
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

        public LaskuPageViewModel(string connectionString)
        {
            _connString = connectionString;

            // Alustetaan PDF-komento
            LuoPdfLaskuCommand = new Command(async () => await GeneroiPdfAsync());

            PoistaLaskuCommand = new Command(async () =>
            {
                if (ValittuLasku == null) return;
                bool ok = await Application.Current.MainPage.DisplayAlert("Vahvista", $"Poistetaanko lasku #{ValittuLasku.Id}?", "Kyllä", "Ei");
                if (!ok) return;
                Laskut.Remove(ValittuLasku);
                ValittuLasku = null;
            });
        }

        // UUSI METODI: PDF-generointi
        private async Task GeneroiPdfAsync()
        {
            if (ValittuLasku == null)
            {
                await Application.Current.MainPage.DisplayAlert("Virhe", "Valitse lasku listasta ensin!", "OK");
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
                await Application.Current.MainPage.DisplayAlert("Virhe", "PDF:n luonti epäonnistui: " + ex.Message, "OK");
            }
        }

        public async Task LataaLaskutAsync()
        {
            try
            {
                Laskut.Clear();

                await using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                const string sql = @"
                    SELECT l.*, v.varattu_pvm, v.varattu_alkupvm, v.varattu_loppupvm,
                           a.etunimi, a.sukunimi, m.mokkinimi
                    FROM lasku l
                    LEFT JOIN varaus v ON v.varaus_id = l.varaus_id
                    LEFT JOIN asiakas a ON a.asiakas_id = v.asiakas_id
                    LEFT JOIN mokki m ON m.mokki_id = v.mokki_id
                    ORDER BY l.lasku_id DESC";

                await using var cmd = new MySqlCommand(sql, conn);
                await using var rdr = await cmd.ExecuteReaderAsync();

                while (await rdr.ReadAsync())
                {
                    var item = new LaskuItem
                    {
                        Id = Convert.ToInt32(rdr["lasku_id"]),
                        VarausId = Convert.ToInt32(rdr["varaus_id"]),
                        Summa = Convert.ToDouble(rdr["summa"]),
                        Alv = Convert.ToDouble(rdr["alv"]),
                        // Tärkeä muunnos SByte -> Double
                        Maksettu = Convert.ToBoolean(rdr["maksettu"]) ? 1.0 : 0.0,
                        AsiakasNimi = $"{rdr["etunimi"]} {rdr["sukunimi"]}",
                        MokkiNimi = rdr["mokkinimi"]?.ToString() ?? "",
                        VarattuPvm = rdr.IsDBNull(rdr.GetOrdinal("varattu_pvm")) ? "" : rdr.GetDateTime("varattu_pvm").ToString("dd.MM.yyyy")
                        // Lisää tähän muut pvm-kentät jos tarpeen
                    };
                    Laskut.Add(item);
                }
            }
            catch (Exception ex)
            {
                // Hätäcatchi
                await Application.Current.MainPage.DisplayAlert("Latausvirhe",
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