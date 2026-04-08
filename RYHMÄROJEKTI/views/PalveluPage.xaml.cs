namespace RYHMÄROJEKTI.views
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Globalization;
    using System.Net.Http.Json;
    using System.Runtime.CompilerServices;
    using System.Windows.Input;
    using Microsoft.Maui.Controls;
    using System.Threading.Tasks;

    public partial class PalveluPage : ContentPage
    {
        public PalveluPage()
        {
            InitializeComponent();
            BindingContext = new PalveluPageViewModel();
        }

        void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (BindingContext is PalveluPageViewModel vm)
            {
                var text = e.NewTextValue?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(text))
                {
                    _ = vm.LataaPalvelutAsync();
                    return;
                }

                var filtered = new System.Collections.Generic.List<PalveluItem>();
                foreach (var p in vm.Palvelut)
                {
                    if ((p.Nimi ?? "").Contains(text, StringComparison.OrdinalIgnoreCase) ||
                        (p.Sijainti ?? "").Contains(text, StringComparison.OrdinalIgnoreCase) ||
                        (p.Kuvaus ?? "").Contains(text, StringComparison.OrdinalIgnoreCase) ||
                        (p.Hinta.ToString() ?? "").Contains(text, StringComparison.OrdinalIgnoreCase))
                    {
                        filtered.Add(p);
                    }
                }

                vm.Palvelut.Clear();
                foreach (var it in filtered) vm.Palvelut.Add(it);
            }
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
            public ObservableCollection<PalveluItem> Palvelut { get; } = new();

            private PalveluItem _valittuPalvelu;
            public PalveluItem ValittuPalvelu
            {
                get => _valittuPalvelu;
                set { _valittuPalvelu = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasValittuPalvelu)); }
            }

            public bool HasValittuPalvelu => ValittuPalvelu != null;

            public ICommand PoistaPalveluCommand { get; }
            public ICommand LisaaPalveluCommand { get; }

            public PalveluPageViewModel()
            {
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
                            var resp = await ApiClient.Http.DeleteAsync($"/api/palvelu/{ValittuPalvelu.Id.Value}");
                            resp.EnsureSuccessStatusCode();
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

                    var list = await ApiClient.Http.GetFromJsonAsync<List<PalveluDto>>("/api/palvelu");
                    if (list == null) return;

                    foreach (var p in list)
                    {
                        Palvelut.Add(new PalveluItem
                        {
                            Id = p.Id,
                            Nimi = p.Nimi ?? string.Empty,
                            Sijainti = p.AlueNimi ?? string.Empty,
                            Kuvaus = p.Kuvaus ?? string.Empty,
                            Hinta = p.Hinta,
                            Alv = p.Alv
                        });
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

            public double Hinta { get; set; }
            public double Alv { get; set; }

            public string HintaFormatted => Hinta.ToString("0.##", CultureInfo.CurrentCulture);
            public string AlvFormatted => $"{Alv.ToString("0.##", CultureInfo.CurrentCulture)}%";
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