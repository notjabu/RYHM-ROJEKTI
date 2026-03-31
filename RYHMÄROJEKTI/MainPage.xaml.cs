using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RYHMÄROJEKTI;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new MainViewModel();
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
        public ObservableCollection<TilastoRivi> Tilasto { get; } = new();

        public async Task LataaTilastoAsync()
        {
            try
            {
                Tilasto.Clear();

                var list = await ApiClient.Http.GetFromJsonAsync<List<TilastoRivi>>("/api/tilasto");
                if (list == null) return;

                foreach (var rivi in list)
                {
                    System.Diagnostics.Debug.WriteLine($"{rivi.Alue} -> {rivi.Maara}");
                    Tilasto.Add(rivi);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Virhe", "Tilaston lataus epäonnistui: " + ex.Message, "OK");
                System.Diagnostics.Debug.WriteLine("LataaTilastoAsync error: " + ex);
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