using Microsoft.Maui.Controls;
using MySqlConnector;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RYHMÄROJEKTI;

public partial class MainPage : ContentPage
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=vn;User=root;Password=YES123;SslMode=None;AllowPublicKeyRetrieval=True;";

    public MainPage()
    {
        InitializeComponent();
        BindingContext = new MainViewModel(ConnectionString);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is MainViewModel vm)
        {
            await vm.LataaTilastoAsync();
        }
    }

    // 🔹 VIEWMODEL
    class MainViewModel : INotifyPropertyChanged
    {
        private readonly string _connString;

        public ObservableCollection<TilastoRivi> Tilasto { get; } = new();

        public MainViewModel(string connString)
        {
            _connString = connString;
        }

        public async Task LataaTilastoAsync()
        {
            try
            {
                Tilasto.Clear();

                await using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                const string sql = @"
                    SELECT a.nimi AS aluenimi, COUNT(m.mokki_id) AS maara
                    FROM alue a
                    LEFT JOIN mokki m ON m.alue_id = a.alue_id
                    GROUP BY a.alue_id, a.nimi
                    ORDER BY a.nimi";

                await using var cmd = new MySqlCommand(sql, conn);
                await using var rdr = await cmd.ExecuteReaderAsync();

                while (await rdr.ReadAsync())
                {
                    string alue = rdr.IsDBNull("aluenimi") ? "" : rdr.GetString("aluenimi");

                    int maara = rdr.IsDBNull("maara")
                        ? 0
                        : Convert.ToInt32(rdr["maara"]);

                    // Debug (näet Output-ikkunassa)
                    System.Diagnostics.Debug.WriteLine($"{alue} -> {maara}");

                    Tilasto.Add(new TilastoRivi
                    {
                        Alue = alue,
                        Maara = maara
                    });
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Virhe", ex.ToString(), "OK");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // 🔹 MALLI
    class TilastoRivi
    {
        public string Alue { get; set; }
        public int Maara { get; set; }
    }
}