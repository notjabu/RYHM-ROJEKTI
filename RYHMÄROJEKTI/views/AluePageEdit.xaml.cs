using Microsoft.Maui.Storage;
using MySqlConnector;
using System;

namespace RYHMÄROJEKTI.views;
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
        var postitoimipaikka = PostitoimipaikkaEntry.Text?.Trim() ?? string.Empty;
        var postinumero = PostinumeroEntry.Text?.Trim() ?? string.Empty;
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

            MySqlTransaction? transaction = null;
            try
            {
                transaction = await conn.BeginTransactionAsync();

                // Insert into `posti` table (postinro, toimipaikka) if both values provided.
                if (!string.IsNullOrWhiteSpace(postinumero) && !string.IsNullOrWhiteSpace(postitoimipaikka))
                {
                    const string postiSql = "INSERT INTO posti (postinro, toimipaikka) VALUES (@postinro, @toimipaikka) " +
                                            "ON DUPLICATE KEY UPDATE toimipaikka = VALUES(toimipaikka)";
                    await using var postiCmd = new MySqlCommand(postiSql, conn);
                    postiCmd.Transaction = transaction;
                    postiCmd.Parameters.AddWithValue("@postinro", postinumero);
                    postiCmd.Parameters.AddWithValue("@toimipaikka", postitoimipaikka);
                    await postiCmd.ExecuteNonQueryAsync();
                }

                // Insert into `alue` table: nimi, sijainti (store postitoimipaikka here), kuvaus
                const string alueSql = "INSERT INTO alue (nimi, sijainti, kuvaus) VALUES (@nimi, @sijainti, @kuvaus)";
                await using var alueCmd = new MySqlCommand(alueSql, conn);
                alueCmd.Transaction = transaction;
                alueCmd.Parameters.AddWithValue("@nimi", nimi);
                alueCmd.Parameters.AddWithValue("@sijainti", postitoimipaikka);
                alueCmd.Parameters.AddWithValue("@kuvaus", kuvaus);

                var affected = await alueCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();

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
            catch
            {
                if (transaction != null)
                {
                    try { await transaction.RollbackAsync(); } catch { }
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
        }
    }
}