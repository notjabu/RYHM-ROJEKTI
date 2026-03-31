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
                string fileName = $"Lasku_{ValittuLasku.LaskuId}.pdf";
                string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(50);
                        page.Header().Text("VILLAGE NEWBIES - LASKU").FontSize(24).Bold().FontColor(QuestPDF.Helpers.Colors.Blue.Medium);

                        page.Content().PaddingVertical(10).Column(col =>
                        {
                            col.Item().Text($"Laskun numero: {ValittuLasku.LaskuId}");
                            col.Item().Text($"Asiakas: {ValittuLasku.AsiakasNimi}");
                            col.Item().Text($"Mökki: {ValittuLasku.MokkiNimi}");
                            col.Item().PaddingTop(5).Text($"Laskutustapa: {ValittuLasku.Laskutustapa}").Italic();

                            col.Item().PaddingTop(20).LineHorizontal(1);

                            col.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text("Majoitus ja palvelut");
                                row.RelativeItem().AlignRight().Text($"{ValittuLasku.Summa} €");
                            });

                            col.Item().AlignRight().Text($"ALV: {ValittuLasku.Alv} %").FontSize(10);
                            col.Item().PaddingTop(20).Text($"YHTEENSÄ: {ValittuLasku.Summa} €").FontSize(18).Bold();
                        });

                        page.Footer().AlignCenter().Text("Kiitos varauksestasi! Maksuehto 14 vrk.");
                    });
                }).GeneratePdf(filePath);

                // Avataan PDF-tiedosto automaattisesti käyttäjälle
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

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}