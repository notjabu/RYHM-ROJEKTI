using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace RYHMÄROJEKTI.views;

public partial class AluePage : ContentPage
{
<<<<<<< HEAD
=======
    // LOCAL CONNECTION STRING - change credentials before running.
    // For Android emulator connecting to host use Server=10.0.2.2
    private const string ConnectionString = "Server=127.0.0.1;Port=3307;Database=vn;User=root;Password=;SslMode=None;";

>>>>>>> b6bbdf4c78ccac69be220bbb6c26b2955fa82c37
    public AluePage()
    {
        InitializeComponent();
        BindingContext = new AlueViewModel();
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

    class AlueViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<AlueItem> Alueet { get; } = new ObservableCollection<AlueItem>();

        private AlueItem _valittuAlue;
        public AlueItem ValittuAlue
        {
            get => _valittuAlue;
            set { _valittuAlue = value; OnPropertyChanged(); }
        }

        public ICommand LisaaAlueCommand { get; }
        public ICommand PoistaAlueCommand { get; }

        public AlueViewModel()
        {
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

                try
                {
                    if (ValittuAlue.Id.HasValue)
                    {
                        var resp = await ApiClient.Http.DeleteAsync($"/api/alue/{ValittuAlue.Id.Value}");
                        resp.EnsureSuccessStatusCode();
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Virhe", "Poisto tietokannasta epäonnistui: " + ex.Message, "OK");
                }

                Alueet.Remove(ValittuAlue);
                ValittuAlue = null;
            });
        }

        public async Task LataaAlueetAsync()
        {
            try
            {
                Alueet.Clear();

                var list = await ApiClient.Http.GetFromJsonAsync<List<AlueDto>>("/api/alue");
                if (list == null) return;

                foreach (var dto in list)
                {
                    Alueet.Add(new AlueItem
                    {
                        Id = dto.Id,
                        Nimi = dto.Nimi ?? string.Empty,
                        Sijainti = dto.Sijainti ?? string.Empty,
                        Kuvaus = dto.Kuvaus ?? string.Empty,
                        Postinumero = dto.Postinumero ?? string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
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