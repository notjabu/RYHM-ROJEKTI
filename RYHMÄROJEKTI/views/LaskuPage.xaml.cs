using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using MySqlConnector;
using System.Threading.Tasks;

namespace RYHMÄROJEKTI.views;

public partial class LaskuPage : ContentPage
{
    private const string ConnectionString = "Server=127.0.0.1;Port=3306;Database=vn;User=root;Password=YES123;SslMode=None;";

    public LaskuPage()
    {
        InitializeComponent();
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

    async void LisaaLasku_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("LaskuPageEdit");
    }

    async void Muokkaa_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is not LaskuPageViewModel vm || vm.ValittuLasku == null)
        {
            await DisplayAlert("Huom", "Valitse muokattava lasku ensin.", "OK");
            return;
        }

        var lasku = vm.ValittuLasku;
        if (lasku.Id.HasValue)
        {
            await Shell.Current.GoToAsync($"LaskuPageEdit?laskuId={lasku.Id.Value}");
        }
        else
        {
            await Shell.Current.GoToAsync("LaskuPageEdit");
        }
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

        public LaskuPageViewModel(string connectionString)
        {
            _connString = connectionString;

            PoistaLaskuCommand = new Command(async () =>
            {
                if (ValittuLasku == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Huom", "Valitse ensin lasku.", "OK");
                    return;
                }

                bool ok = await Application.Current.MainPage.DisplayAlert(
                    "Vahvista", $"Poistetaanko lasku #{ValittuLasku.Id}?", "Kyllä", "Ei");
                if (!ok) return;

                try
                {
                    if (ValittuLasku.Id.HasValue)
                    {
                        await using var conn = new MySqlConnection(_connString);
                        await conn.OpenAsync();
                        const string sql = "DELETE FROM lasku WHERE lasku_id = @id";
                        await using var cmd = new MySqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@id", ValittuLasku.Id.Value);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Virhe", "Poisto epäonnistui: " + ex.Message, "OK");
                }

                Laskut.Remove(ValittuLasku);
                ValittuLasku = null;
            });
        }

        public async Task LataaLaskutAsync()
        {
            try
            {
                Laskut.Clear();

                await using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                const string sql = @"
                    SELECT l.lasku_id, l.varaus_id, l.summa, l.alv, l.maksettu,
                           v.asiakas_id, v.mokki_id, v.varattu_pvm,
                           v.vahvistus_pvm, v.varattu_alkupvm, v.varattu_loppupvm,
                           a.etunimi, a.sukunimi,
                           m.mokkinimi
                    FROM lasku l
                    LEFT JOIN varaus v ON v.varaus_id = l.varaus_id
                    LEFT JOIN asiakas a ON a.asiakas_id = v.asiakas_id
                    LEFT JOIN mokki m ON m.mokki_id = v.mokki_id
                    ORDER BY l.lasku_id DESC";

                await using var cmd = new MySqlCommand(sql, conn);
                await using var rdr = await cmd.ExecuteReaderAsync();

                while (await rdr.ReadAsync())
                {
                    var etunimi = rdr.IsDBNull(rdr.GetOrdinal("etunimi")) ? string.Empty : rdr.GetString("etunimi");
                    var sukunimi = rdr.IsDBNull(rdr.GetOrdinal("sukunimi")) ? string.Empty : rdr.GetString("sukunimi");

                    var item = new LaskuItem
                    {
                        Id = rdr.IsDBNull(rdr.GetOrdinal("lasku_id")) ? null : rdr.GetInt32("lasku_id"),
                        VarausId = rdr.IsDBNull(rdr.GetOrdinal("varaus_id")) ? null : rdr.GetInt32("varaus_id"),
                        Summa = rdr.IsDBNull(rdr.GetOrdinal("summa")) ? 0 : rdr.GetDouble("summa"),
                        Alv = rdr.IsDBNull(rdr.GetOrdinal("alv")) ? 0 : rdr.GetDouble("alv"),
                        Maksettu = rdr.IsDBNull(rdr.GetOrdinal("maksettu")) ? 0.0 : rdr.GetDouble("maksettu"),
                        AsiakasNimi = $"{etunimi} {sukunimi}".Trim(),
                        MokkiNimi = rdr.IsDBNull(rdr.GetOrdinal("mokkinimi")) ? string.Empty : rdr.GetString("mokkinimi"),
                        VarattuPvm = rdr.IsDBNull(rdr.GetOrdinal("varattu_pvm")) ? string.Empty : rdr.GetDateTime("varattu_pvm").ToString("dd.MM.yyyy"),
                        VarattuAlkuPvm = rdr.IsDBNull(rdr.GetOrdinal("varattu_alkupvm")) ? string.Empty : rdr.GetDateTime("varattu_alkupvm").ToString("dd.MM.yyyy"),
                        VarattuLoppuPvm = rdr.IsDBNull(rdr.GetOrdinal("varattu_loppupvm")) ? string.Empty : rdr.GetDateTime("varattu_loppupvm").ToString("dd.MM.yyyy")
                    };

                    Laskut.Add(item);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Virhe", "Laskujen lataus epäonnistui: " + ex.Message, "OK");
                System.Diagnostics.Debug.WriteLine("LataaLaskutAsync error: " + ex);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ── Model ───────────────────────────────────────────────────────────
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
        public string VarattuAlkuPvm { get; set; } = string.Empty;
        public string VarattuLoppuPvm { get; set; } = string.Empty;
        public double Maksamatta => Summa - Maksettu;
        public string Otsikko => $"Lasku #{Id} – {AsiakasNimi}";
        public string SummaText => $"{Summa:F2} € (ALV {Alv}%)";
        public string MaksettuText => $"{Maksettu:F2} €";
        public string MaksamattaText => $"{Maksamatta:F2} €";
    }
}