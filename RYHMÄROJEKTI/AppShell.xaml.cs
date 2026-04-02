using RYHMÄROJEKTI.views;
using Microsoft.Maui.Controls;

namespace RYHMÄROJEKTI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(PalveluPageEdit), typeof(PalveluPageEdit));
            Routing.RegisterRoute(nameof(AluePageEdit), typeof(AluePageEdit));
            Routing.RegisterRoute(nameof(MokkiPageEdit), typeof(MokkiPageEdit));
            Routing.RegisterRoute(nameof(AsiakasPageEdit), typeof(AsiakasPageEdit));
            Routing.RegisterRoute(nameof(LaskuPageEdit), typeof(LaskuPageEdit));
            Routing.RegisterRoute(nameof(VarausPageEdit), typeof(VarausPageEdit));
        }
    }
}
