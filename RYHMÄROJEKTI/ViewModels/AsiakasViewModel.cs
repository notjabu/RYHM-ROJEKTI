using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RYHMÄROJEKTI.Models;

namespace RYHMÄROJEKTI.ViewModels
{
    public class AsiakasViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Asiakas> Asiakkaat { get; set; } = new ObservableCollection<Asiakas>();

        private Asiakas _valittuAsiakas;
        public Asiakas ValittuAsiakas
        {
            get => _valittuAsiakas;
            set { _valittuAsiakas = value; OnPropertyChanged(); }
        }

        public ICommand LisaaAsiakasCommand { get; }
        public ICommand PoistaAsiakasCommand { get; }

        public AsiakasViewModel()
        {
            // Testidataa
            Asiakkaat.Add(new Asiakas { Etunimi = "Matti", Sukunimi = "Meikäläinen", Sahkoposti = "matti@esimerkki.fi" });
            Asiakkaat.Add(new Asiakas { Etunimi = "Maija", Sukunimi = "Meikäläinen", Sahkoposti = "maija@esimerkki.fi" });

            LisaaAsiakasCommand = new Command(async () => {
                await Application.Current.MainPage.DisplayAlert("Asiakashallinta", "Tästä avautuisi asiakkaan lisäys", "OK");
            });

            PoistaAsiakasCommand = new Command(() => {
                if (ValittuAsiakas != null) Asiakkaat.Remove(ValittuAsiakas);
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}