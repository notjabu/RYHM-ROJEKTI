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
        }
    }
}
