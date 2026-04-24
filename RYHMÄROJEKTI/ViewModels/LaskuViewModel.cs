using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RYHMÄROJEKTI.Modeelit;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace RYHMÄROJEKTI.ViewModels
{
    public class LaskuViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Lasku> Laskut { get; set; } = new ObservableCollection<Lasku>();

        private Lasku _valittuLasku;
        public Lasku ValittuLasku
        {
            get => _valittuLasku;
            set { _valittuLasku = value; OnPropertyChanged(); }
        }

        public ICommand LuoPdfLaskuCommand { get; }

        public LaskuViewModel()
        {
            // Asetetaan QuestPDF-lisenssi
            QuestPDF.Settings.License = LicenseType.Community;

            LuoPdfLaskuCommand = new Command(async () => await GeneroiLaskuPdf());
        }

        private async Task GeneroiLaskuPdf()
        {
            if (ValittuLasku == null) return;
           
            try
            {
                // Luodaan uniikki nimi, jotta välimuisti ei temppuile
                string aikaleima = DateTime.Now.ToString("HHmmss");
                string uniqueId = Guid.NewGuid().ToString().Substring(0, 4);
                string tyopoyta = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                
                string fileName = $"LASKU_TESTI_{DateTime.Now:HHmmss}.pdf";
                string filePath = Path.Combine(tyopoyta, fileName);

                var dokumentti = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(50);

                        // Otsikko
                        page.Header().Column(col =>
                        {
                            col.Item().Text("TÄMÄ ON UUSI VERSIO").FontSize(20).FontColor(QuestPDF.Helpers.Colors.Red.Medium);
                            col.Item().Text("VILLAGE NEWBIES - LASKU").FontSize(24).Bold().FontColor(QuestPDF.Helpers.Colors.Blue.Medium);
                        });

                        page.Content().PaddingVertical(10).Column(col =>
                        {
                            col.Item().Text($"Laskun numero: {ValittuLasku.LaskuId}");
                            col.Item().Text($"Asiakas: {ValittuLasku.AsiakasNimi}");
                            col.Item().Text($"Mökki: {ValittuLasku.MokkiNimi}");
                            col.Item().PaddingTop(5).Text($"Laskutustapa: {ValittuLasku.Laskutustapa}").Italic();

                            col.Item().PaddingTop(20).LineHorizontal(1);

                            col.Item().PaddingTop(10).Text($"Summa: {ValittuLasku.Summa} €");
                            col.Item().Text($"ALV: {ValittuLasku.Alv} %");
                            col.Item().PaddingTop(10).Text($"YHTEENSÄ: {ValittuLasku.Summa} €").FontSize(18).Bold();

                            col.Item().PaddingTop(30).Text("MAKSUYHTEYSTIEDOT").Bold().Underline();
                            col.Item().Text("Saaja: Village Newbies Oy");
                            col.Item().Text("IBAN: FI12 3456 7890 1234 56");
                            col.Item().Text("BIC: OKOYFIHH");
                            col.Item().Text("Eräpäivä: 14 vuorokautta laskun päiväyksestä");
                        });

                        // Sivunumerointi
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Sivu ");
                            x.CurrentPageNumber();
                        });
                    });
                });

                dokumentti.GeneratePdf(filePath);

                // Avataan PDF
                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(filePath)
                });
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Virhe", $"PDF-luonti epäonnistui: {ex.Message}", "OK");
            }
        }
       private async Task LahetaLaskuSahkopostilla()
        {
            if (ValittuLasku == null) return;
            await Application.Current.MainPage.DisplayAlert("Sähköpostilasku",
                $"Lasku {ValittuLasku.LaskuId} lähetetty asiakkaalle!", "OK");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}