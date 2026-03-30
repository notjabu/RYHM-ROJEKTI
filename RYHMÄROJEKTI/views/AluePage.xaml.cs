using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using MySqlConnector;
using System.Threading.Tasks;

namespace RYHMÄROJEKTI.views;

public partial class AluePage : ContentPage
{
    // LOCAL CONNECTION STRING - change credentials before running.
    // For Android emulator connecting to host use Server=10.0.2.2
    private const string ConnectionString = "Server=127.0.0.1;Port=3306;Database=vn;User=root;Password=YES123;SslMode=None;";

    public AluePage()
    {
        InitializeComponent();

        // Set a simple ViewModel that loads `alue` + `posti` data required by the XAML.
        BindingContext = new AlueViewModel(ConnectionString);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Load data when page appears
        if (BindingContext is AlueViewModel vm)
        {
            await vm.LataaAlueetAsync();
        }
    }

    async void LisaaAlue_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AluePageEdit");
    }

    async void Muokkaa_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is not AlueViewModel vm || vm.ValittuAlue == null)
        {
            await DisplayAlert("Huom", "Valitse muokattava alue ensin.", "OK");
            return;
        }

        var alue = vm.ValittuAlue;
        if (alue.Id.HasValue)
        {
            await Shell.Current.GoToAsync($"AluePageEdit?alueId={alue.Id.Value}");
        }
        else
        {
            await Shell.Current.GoToAsync("AluePageEdit");
        }
    }

    // Simple ViewModel used by the page (keeps everything in one file so you only needed to change this file)
    class AlueViewModel : INotifyPropertyChanged
    {
        private readonly string _connString;

        public ObservableCollection<AlueItem> Alueet { get; } = new ObservableCollection<AlueItem>();

        private AlueItem _valittuAlue;
        public AlueItem ValittuAlue
        {
            get => _valittuAlue;
            set { _valittuAlue = value; OnPropertyChanged(); }
        }

        public ICommand LisaaAlueCommand { get; }
        public ICommand PoistaAlueCommand { get; }

        public AlueViewModel(string connectionString)
        {
            _connString = connectionString;

            LisaaAlueCommand = new Command(async () =>
            {
                await Shell.Current.GoToAsync("AluePageEdit");
            });

            PoistaAlueCommand = new Command(async () =>
            {
                if (ValittuAlue == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Huom", "Valitse ensin alue.", "OK");
                    return;
                }

                bool ok = await Application.Current.MainPage.DisplayAlert("Vahvista", $"Poistetaanko alue \"{ValittuAlue.Nimi}\"?", "Kyllä", "Ei");
                if (!ok) return;

                // Try to remove from DB if Id present; otherwise just remove from collection
                try
                {
                    if (ValittuAlue.Id.HasValue)
                    {
                        await using var conn = new MySqlConnection(_connString);
                        await conn.OpenAsync();
                        const string delSql = "DELETE FROM alue WHERE alue_id = @id";
                        await using var cmd = new MySqlCommand(delSql, conn);
                        cmd.Parameters.AddWithValue("@id", ValittuAlue.Id.Value);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception ex)
                {
                    // Non-fatal: show error but continue removing locally so UI remains responsive
                    await Application.Current.MainPage.DisplayAlert("Virhe", "Poisto tietokannasta epäonnistui: " + ex.Message, "OK");
                }

                Alueet.Remove(ValittuAlue);
                ValittuAlue = null;
            });
        }

        // Loads areas and associated posti.postinro (if any). Matches XAML bindings:
        // ValittuAlue.Nimi, ValittuAlue.Sijainti, ValittuAlue.Postinumero, ValittuAlue.Kuvaus
        public async Task LataaAlueetAsync()
        {
            try
            {
                Alueet.Clear();

                await using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                // Include posti.postinro in the SELECT so Postinumero comes from posti table.
                const string sql = @"
                    SELECT a.alue_id, a.nimi, a.sijainti, a.kuvaus, p.postinro
                    FROM alue a
                    LEFT JOIN posti p ON p.toimipaikka = a.sijainti
                    ORDER BY a.nimi";

                await using var cmd = new MySqlCommand(sql, conn);
                await using var rdr = await cmd.ExecuteReaderAsync();

                while (await rdr.ReadAsync())
                {
                    var item = new AlueItem
                    {
                        Id = rdr.IsDBNull(rdr.GetOrdinal("alue_id")) ? null : rdr.GetInt32("alue_id"),
                        Nimi = rdr.IsDBNull(rdr.GetOrdinal("nimi")) ? string.Empty : rdr.GetString("nimi"),
                        Sijainti = rdr.IsDBNull(rdr.GetOrdinal("sijainti")) ? string.Empty : rdr.GetString("sijainti"),
                        Kuvaus = rdr.IsDBNull(rdr.GetOrdinal("kuvaus")) ? string.Empty : rdr.GetString("kuvaus"),
                        Postinumero = rdr.IsDBNull(rdr.GetOrdinal("postinro")) ? string.Empty : rdr.GetString("postinro")
                    };

                    Alueet.Add(item);
                }
            }
            catch (Exception ex)
            {
                // Show a friendly message and write debug output
                await Application.Current.MainPage.DisplayAlert("Virhe", "Alueiden lataus epäonnistui: " + ex.Message, "OK");
                System.Diagnostics.Debug.WriteLine("LataaAlueetAsync error: " + ex);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Model used by the ViewModel and bound in XAML
    class AlueItem
    {
        public int? Id { get; set; }                 // optional, used for delete if present
        public string Nimi { get; set; }
        public string Sijainti { get; set; }        // mapped to alue.sijainti
        public string Postinumero { get; set; }     // from posti.postinro (joined)
        public string Kuvaus { get; set; }
        public string Kuva { get; set; }            // optional image path/url used in list thumbnail
    }
}