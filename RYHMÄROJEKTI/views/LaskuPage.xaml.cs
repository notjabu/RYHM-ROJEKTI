using System;
using System.Collections;
using System.Reflection;
using Microsoft.Maui.Controls;

namespace RYHMÄROJEKTI.views;

public partial class LaskuPage : ContentPage
{
    public LaskuPage()
    {
        InitializeComponent();
    }

    async void NaytaLaskut_Clicked(object sender, EventArgs e)
    {
        var bc = BindingContext;
        var valittu = bc?.GetType().GetProperty("ValittuAsiakas")?.GetValue(bc);
        if (valittu == null)
        {
            await DisplayAlert("Huom", "Valitse asiakas nähdäksesi laskut.", "OK");
            return;
        }

        var laskutProp = valittu.GetType().GetProperty("Laskut");
        if (laskutProp == null)
        {
            await DisplayAlert("Info", "Valitulla asiakkaalla ei ole laskut-kokoelmaa.", "OK");
            return;
        }

        var laskut = laskutProp.GetValue(valittu) as ICollection;
        int count = laskut?.Count ?? 0;
        await DisplayAlert("Laskut", $"Valitulla asiakkaalla on {count} laskua.", "OK");
    }

    async void LisaaLasku_Clicked(object sender, EventArgs e)
    {
        var bc = BindingContext;
        var valittu = bc?.GetType().GetProperty("ValittuAsiakas")?.GetValue(bc);
        if (valittu == null)
        {
            await DisplayAlert("Huom", "Valitse asiakas ennen laskun lisäämistä.", "OK");
            return;
        }

        
        await DisplayAlert("Lisää lasku", "Lisää lasku -toiminto ei ole vielä toteutettu.", "OK");
    }

    async void MuokkaaLasku_Clicked(object sender, EventArgs e)
    {
        var selectedInvoice = InvoiceListView.SelectedItem;
        if (selectedInvoice == null)
        {
            await DisplayAlert("Huom", "Valitse muokattava lasku oikeasta listasta.", "OK");
            return;
        }

        
        await DisplayAlert("Muokkaa laskua", "Muokkaa laskua -toiminto ei ole vielä toteutettu.", "OK");
    }

    async void PoistaLasku_Clicked(object sender, EventArgs e)
    {
        var bc = BindingContext;
        var valittuAsiakas = bc?.GetType().GetProperty("ValittuAsiakas")?.GetValue(bc);
        if (valittuAsiakas == null)
        {
            await DisplayAlert("Huom", "Valitse asiakas ennen poistamista.", "OK");
            return;
        }

        var selectedInvoice = InvoiceListView.SelectedItem;
        if (selectedInvoice == null)
        {
            await DisplayAlert("Huom", "Valitse poistettava lasku oikeasta listasta.", "OK");
            return;
        }

        
        var numero = selectedInvoice.GetType().GetProperty("Numero")?.GetValue(selectedInvoice)?.ToString() ?? selectedInvoice.ToString();
        bool vahvista = await DisplayAlert("Vahvista", $"Poistetaanko lasku {numero}?", "Poista", "Peruuta");
        if (!vahvista) return;

        
        var laskutProp = valittuAsiakas.GetType().GetProperty("Laskut");
        if (laskutProp == null)
        {
            await DisplayAlert("Error", "Asiakkaalta puuttuu 'Laskut' -kokoelma.", "OK");
            return;
        }

        var laskutCollection = laskutProp.GetValue(valittuAsiakas);
        if (laskutCollection == null)
        {
            await DisplayAlert("Error", "'Laskut' on null.", "OK");
            return;
        }

        if (laskutCollection is IList list)
        {
            list.Remove(selectedInvoice);
        }
        else
        {
            var removeMethod = laskutCollection.GetType().GetMethod("Remove", new Type[] { selectedInvoice.GetType() })
                               ?? laskutCollection.GetType().GetMethod("Remove", new Type[] { typeof(object) })
                               ?? laskutCollection.GetType().GetMethod("Remove");
            if (removeMethod != null)
            {
                removeMethod.Invoke(laskutCollection, new object[] { selectedInvoice });
            }
            else
            {
                await DisplayAlert("Error", "Kokoelmaa ei voi muokata (Remove-metodia ei löydy).", "OK");
                return;
            }
        }

        
        var vmProp = bc.GetType().GetProperty("ValittuLasku");
        vmProp?.SetValue(bc, null);

        await DisplayAlert("Valmis", $"Lasku {numero} poistettu.", "OK");
    }
}