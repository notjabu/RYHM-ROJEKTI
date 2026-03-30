namespace RYHMÄROJEKTI.views
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Windows.Input;
    using Microsoft.Maui.Controls;
    using MySqlConnector;
    using System.Threading.Tasks;

    public partial class PalveluPage : ContentPage
    {
        private const string ConnectionString = "Server=127.0.0.1;Port=3306;Database=vn;User=root;Password=YES123;SslMode=None;";

        public PalveluPage()
        {
            InitializeComponent();
            BindingContext = new PalveluPageViewModel(ConnectionString);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is PalveluPageViewModel vm)
            {
                await vm.LataaPalvelutAsync();
            }
        }

        async void MuokkaaPalveluClicked(object sender, EventArgs e)
        {
            if (BindingContext is not PalveluPageViewModel vm || vm.ValittuPalvelu == null)
            {
                await DisplayAlert("Huom", "Valitse muokattava palvelu ensin.", "OK");
                return;
            }

            var item = vm.ValittuPalvelu;
            if (item.Id.HasValue)
                await Shell.Current.GoToAsync($"PalveluPageEdit?palveluId={item.Id.Value}");
            else
                await Shell.Current.GoToAsync("PalveluPageEdit");
        }

        // ── ViewModel ───────────────────────────────────────────────────────
        class PalveluPageViewModel : INotifyPropertyChanged
        {
            private readonly string _connString;

            public ObservableCollection<PalveluItem> Palvelut { get; } = new();

            private PalveluItem _valittuPalvelu;
            public PalveluItem ValittuPalvelu
            {
                get => _valittuPalvelu;
                set { _valittuPalvelu = value; OnPropertyChanged(); }
            }

            public ICommand PoistaPalveluCommand { get; }
            public ICommand LisaaPalveluCommand { get; }

            public PalveluPageViewModel(string connectionString)
            {
                _connString = connectionString;

                LisaaPalveluCommand = new Command(async () =>
                {
                    await Shell.Current.GoToAsync("PalveluPageEdit");
                });

                PoistaPalveluCommand = new Command(async () =>
                {
                    if (ValittuPalvelu == null)
                    {
                        await Application.Current.MainPage.DisplayAlert("Huom", "Valitse ensin palvelu.", "OK");
                        return;
                    }

                    bool ok = await Application.Current.MainPage.DisplayAlert(
                        "Vahvista", $"Poistetaanko palvelu \"{ValittuPalvelu.Nimi}\"?", "Kyllä", "Ei");
                    if (!ok) return;

                    try
                    {
                        if (ValittuPalvelu.Id.HasValue)
                        {
                            await using var conn = new MySqlConnection(_connString);
                            await conn.OpenAsync();
                            const string sql = "DELETE FROM palvelu WHERE palvelu_id = @id";
                            await using var cmd = new MySqlCommand(sql, conn);
                            cmd.Parameters.AddWithValue("@id", ValittuPalvelu.Id.Value);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Virhe", "Poisto epäonnistui: " + ex.Message, "OK");
                    }

                    Palvelut.Remove(ValittuPalvelu);
                    ValittuPalvelu = null;
                });
            }

            public async Task LataaPalvelutAsync()
            {
                try
                {
                    Palvelut.Clear();

                    await using var conn = new MySqlConnection(_connString);
                    await conn.OpenAsync();

                    const string sql = @"
                        SELECT palvelu_id, nimi, kuvaus
                        FROM palvelu
                        ORDER BY nimi";

                    await using var cmd = new MySqlCommand(sql, conn);
                    await using var rdr = await cmd.ExecuteReaderAsync();

                    while (await rdr.ReadAsync())
                    {
                        var item = new PalveluItem
                        {
                            Id = rdr.IsDBNull(rdr.GetOrdinal("palvelu_id")) ? null : rdr.GetInt32("palvelu_id"),
                            Nimi = rdr.IsDBNull(rdr.GetOrdinal("nimi")) ? string.Empty : rdr.GetString("nimi"),
                            Sijainti = string.Empty,
                            Kuvaus = rdr.IsDBNull(rdr.GetOrdinal("kuvaus")) ? string.Empty : rdr.GetString("kuvaus")
                        };

                        Palvelut.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Virhe", "Palveluiden lataus epäonnistui: " + ex.Message, "OK");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            void OnPropertyChanged([CallerMemberName] string name = null) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // ── Model ───────────────────────────────────────────────────────────
        class PalveluItem
        {
            public int? Id { get; set; }
            public string Nimi { get; set; } = string.Empty;
            public string Sijainti { get; set; } = string.Empty;
            public string Kuvaus { get; set; } = string.Empty;
        }
    }

    // Simple converter used by the page to turn null -> false, non-null -> true
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}