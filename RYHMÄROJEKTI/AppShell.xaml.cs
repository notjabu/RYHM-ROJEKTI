using RYHMÄROJEKTI.views;
using Microsoft.Maui.Controls;
using System.ComponentModel;

namespace RYHMÄROJEKTI
{
    public partial class AppShell : Shell, INotifyPropertyChanged
    {
        private string _currentTime = DateTime.Now.ToString("dd.MM.yyyy, klo HH.mm");
        public string CurrentTime
        {
            get => _currentTime;
            set
            {
                if (_currentTime == value) return;
                _currentTime = value;
                OnPropertyChanged();
            }
        }

        private string _pageTitle = "Village Newbies";
        public string PageTitle
        {
            get => _pageTitle;
            set
            {
                if (_pageTitle == value) return;
                _pageTitle = value;
                OnPropertyChanged();
            }
        }

        public AppShell()
        {
            InitializeComponent();

            BindingContext = this;

            // Update clock every second
            var timer = Application.Current.Dispatcher.CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => CurrentTime = DateTime.Now.ToString("dd.MM.yyyy, klo HH.mm");
            timer.Start();

            // Update the page title whenever navigation occurs
            Navigated += (s, e) =>
            {
                PageTitle = CurrentPage?.Title ?? "Village Newbies";
            };

            Routing.RegisterRoute(nameof(PalveluPageEdit), typeof(PalveluPageEdit));
            Routing.RegisterRoute(nameof(AluePageEdit), typeof(AluePageEdit));
            Routing.RegisterRoute(nameof(MokkiPageEdit), typeof(MokkiPageEdit));
            Routing.RegisterRoute(nameof(AsiakasPageEdit), typeof(AsiakasPageEdit));
            Routing.RegisterRoute(nameof(LaskuPageEdit), typeof(LaskuPageEdit));
            Routing.RegisterRoute(nameof(VarausPageEdit), typeof(VarausPageEdit));
        }
    }
}
