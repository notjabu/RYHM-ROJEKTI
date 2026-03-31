using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RYHMÄROJEKTI.Modeelit;

namespace RYHMÄROJEKTI.ViewModels
{
    public class MokkiViewModel : INotifyPropertyChanged
    {
        // Lista, joka näkyy käyttöliittymässä
        public ObservableCollection<Mokki> Mokit { get; set; } = new ObservableCollection<Mokki>();

        private Mokki _valittuMokki;
        public Mokki ValittuMokki
        {
            get => _valittuMokki;
            set
            {
                _valittuMokki = value;
                OnPropertyChanged();
            }
        }

        // Komennot napeille
        public ICommand LisaaMokkiCommand { get; }
        public ICommand PoistaMokkiCommand { get; }
        public ICommand TallennaMuutoksetCommand { get; }

        public MokkiViewModel()
        {
            // Komento uuden tyhjän mökin luomiseen listalle
            LisaaMokkiCommand = new Command(() => {
                var uusiMokki = new Mokki { Nimi = "Uusi Mökki" };
                Mokit.Add(uusiMokki);
                ValittuMokki = uusiMokki;
            });

            // Komento valitun mökin tallentamiseen tietokantaan
            TallennaMuutoksetCommand = new Command(async () => {
                if (ValittuMokki != null)
                {
                    try
                    {
                        await ApiClient.Http.PostAsJsonAsync("/api/mokki", ValittuMokki);
                        await Application.Current.MainPage.DisplayAlert("Tallennus", "Mökki tallennettu tietokantaan!", "OK");
                        // Päivitetään lista, jotta ID:t ja muutokset näkyvät oikein
                        await LataaMokit();
                    }
                    catch (Exception ex)
                    {
                        await Application.Current.MainPage.DisplayAlert("Virhe", "Tallennus epäonnistui: " + ex.Message, "OK");
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Huom", "Valitse ensin mökki listasta.", "OK");
                }
            });

            // Komento mökin poistamiseen
            PoistaMokkiCommand = new Command(async () => {
                if (ValittuMokki != null)
                {
                    bool vastaus = await Application.Current.MainPage.DisplayAlert("Vahvistus", "Haluatko varmasti poistaa mökin?", "Kyllä", "Ei");
                    if (vastaus)
                    {
                        Mokit.Remove(ValittuMokki);
                        ValittuMokki = null;
                    }
                }
            });
        }

        // Metodi mökkien lataamiseen tietokannasta (kutsutaan MokkiPage.xaml.cs:stä)
        public async Task LataaMokit()
        {
            try
            {
                var tietokantaMokit = await ApiClient.Http.GetFromJsonAsync<List<Mokki>>("/api/mokki") ?? new();

                Mokit.Clear();
                foreach (var mokki in tietokantaMokit)
                {
                    Mokit.Add(mokki);
                }
            }
            catch (Exception ex)
            {
                // Jos tietokanta ei ole vielä valmis tms.
                System.Diagnostics.Debug.WriteLine("Virhe ladattaessa mökkejä: " + ex.Message);
            }
        }

        // PropertyChanged-logiikka, jotta UI päivittyy
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}