using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace RYHMÄROJEKTI.views;

public partial class LaskuPage : ContentPage
{
    public LaskuPage()
    {
        InitializeComponent();
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
        public ObservableCollection<LaskuItem> Laskut { get; } = new();

        private LaskuItem _valittuLasku;
        public LaskuItem ValittuLasku
        {
            get => _valittuLasku;
            set { _valittuLasku = value; OnPropertyChanged(); }
        }

        public ICommand PoistaLaskuCommand { get; }

        public LaskuPageViewModel()
        {
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
                        var resp = await ApiClient.Http.DeleteAsync($"/api/lasku/{ValittuLasku.Id.Value}");
                        resp.EnsureSuccessStatusCode();
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
                        VarattuAlkuPvm = dto.VarattuAlkuPvm?.ToString("dd.MM.yyyy") ?? string.Empty,
                        VarattuLoppuPvm = dto.VarattuLoppuPvm?.ToString("dd.MM.yyyy") ?? string.Empty
                    });
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