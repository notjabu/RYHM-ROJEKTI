using Microsoft.Maui.Storage;
using MySqlConnector;
using RYHMÄROJEKTI.views;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RYHMÄROJEKTI.views;

public partial class MokkiPageEdit : ContentPage
{
    private const string ConnectionString = "Server=127.0.0.1;Port=3306;Database=vn;User=root;Password=YES123;SslMode=None;";

    public MokkiPageEdit()
    {
        InitializeComponent();
    }

    async void OnPickImageClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Valitse mökille kuva"
            });

            if (result == null)
                return;

            var stream = await result.OpenReadAsync();
            var destPath = Path.Combine(FileSystem.CacheDirectory, result.FileName);
            using (var dest = File.OpenWrite(destPath))
            {
                await stream.CopyToAsync(dest);
            }

           
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", $"Kuvan valinta epäonnistui: {ex.Message}", "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var nimi = NimiEntry.Text?.Trim() ?? string.Empty;
        var katu = KatuEntry.Text?.Trim() ?? string.Empty;
        var postinumero = PostinumeroEntry.Text?.Trim() ?? string.Empty;
        var postitoimipaikka = PostitoimipaikkaEntry.Text?.Trim() ?? string.Empty;
        var kuvaus = KuvausEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(nimi))
        {
            await DisplayAlert("Virhe", "Anna nimi.", "OK");
            return;
        }
        if (string.IsNullOrEmpty(katu))
        {
            await DisplayAlert("Virhe", "Anna katu", "OK");
            return;
        }

        try
        {
            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sql = "INSERT INTO Alue (MokkiNimi, Katuosoite, Kuvaus, Postinumero) VALUES (@mokkinimi, @katuosoite, @kuvaus, @postinumero)";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@mokkinimi", nimi);
            cmd.Parameters.AddWithValue("@katuosoite", katu);
            cmd.Parameters.AddWithValue("@postinumero", postinumero);
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
        
  private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}