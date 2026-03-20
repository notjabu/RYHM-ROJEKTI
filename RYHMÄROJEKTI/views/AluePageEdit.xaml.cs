namespace RYHMÄROJEKTI.views;
using MySqlConnector;
public partial class AluePageEdit : ContentPage
{
    // LOCAL CONNECTION STRING - change credentials before running.
    // For Android emulator connecting to host use Server=10.0.2.2
    private const string ConnectionString = "Server=127.0.0.1;Port=3306;Database=vn;User=root;Password=YES123;SslMode=None;";

    public AluePageEdit()
    {
        InitializeComponent();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnSelectImageClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Valitse kuva", FileTypes = FilePickerFileType.Images });
            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                KuvaImage.Source = ImageSource.FromStream(() => stream);
                // Decide whether to upload image or store local path in DB.
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", ex.Message, "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var nimi = NimiEntry.Text?.Trim() ?? string.Empty;
        var sijainti = SijaintiEntry.Text?.Trim() ?? string.Empty;
        var kuvaus = KuvausEditor.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(nimi))
        {
            await DisplayAlert("Virhe", "Anna nimi.", "OK");
            return;
        }

        try
        {
            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sql = "INSERT INTO Alue (Nimi, Sijainti, Kuvaus) VALUES (@nimi, @sijainti, @kuvaus)";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@nimi", nimi);
            cmd.Parameters.AddWithValue("@sijainti", sijainti);
            cmd.Parameters.AddWithValue("@kuvaus", kuvaus);

            var affected = await cmd.ExecuteNonQueryAsync();
            if (affected > 0)
            {
                await DisplayAlert("Valmis", "Alue tallennettu.", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Virhe", "Tallennus epäonnistui.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
        }
    }
}