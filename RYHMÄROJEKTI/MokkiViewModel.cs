using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using RYHMÄROJEKTI.Models;
using RYHMÄROJEKTI.views;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

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

                // Wait for the edit page to signal completion
                var result = await editPage.Completion;
                if (result != null)
                {
                    Mokit.Add(result);
                    ValittuMokki = result; // select it so Tietoikkuna shows details
                }
            });

            MuokkaaMokkiaCommand = new Command(async () =>
            {
                if (ValittuMokki == null) return;
                var editPage = new MokkiPageEdit(ValittuMokki);
                await Application.Current.MainPage.Navigation.PushAsync(editPage);

                // Optionally await completion if you want to react (e.g., refresh selection).
                var edited = await editPage.Completion;
                if (edited != null)
                {
                    // ValittuMokki is the same reference; ensure UI sees changes
                    ValittuMokki = edited;
                }
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