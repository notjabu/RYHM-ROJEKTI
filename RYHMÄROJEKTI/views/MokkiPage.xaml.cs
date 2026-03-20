using RYHMÄROJEKTI.ViewModels;
namespace RYHMÄROJEKTI.views;

public partial class MokkiPage : ContentPage
{
	public MokkiPage()
	{
		InitializeComponent();
        BindingContext = new MokkiViewModel();
    }
}