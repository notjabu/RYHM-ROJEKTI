using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using RYHMÄROJEKTI.Models;
using RYHMÄROJEKTI.views;

namespace RYHMÄROJEKTI.ViewModels
{
    public class MokkiViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Mokki> Mokit { get; } = new ObservableCollection<Mokki>();

        private Mokki _valittuMokki;
        public Mokki ValittuMokki { get => _valittuMokki; set { _valittuMokki = value; OnPropertyChanged(); } }

        public ICommand LisaaMokkiCommand { get; }
        public ICommand MuokkaaMokkiaCommand { get; }
        public ICommand PoistaMokkiCommand { get; }

        public MokkiViewModel()
        {
            LisaaMokkiCommand = new Command(async () =>
            {
                var editPage = new MokkiPageEdit(new Mokki());
                await Application.Current.MainPage.Navigation.PushAsync(editPage);
                // odota kunnes sivulta palataan — Result asetetaan Save-painikkeella
                // kun sivu suljetaan, lisätään, jos käyttäjä tallensi
                if (editPage.Result != null)
                {
                    Mokit.Add(editPage.Result);
                }
            });

            MuokkaaMokkiaCommand = new Command(async () =>
            {
                if (ValittuMokki == null) return;
                var editPage = new MokkiPageEdit(ValittuMokki);
                await Application.Current.MainPage.Navigation.PushAsync(editPage);
                // muokkaukset tehdään suoraan ValittuMokki-olioon (reference)
            });

            PoistaMokkiCommand = new Command(() =>
            {
                if (ValittuMokki != null)
                    Mokit.Remove(ValittuMokki);
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}