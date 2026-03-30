using Microsoft.Maui.Storage;
using MySqlConnector;
using System;
using System.Collections.Generic;

namespace RYHMÄROJEKTI.views;

[QueryProperty(nameof(AlueId), "alueId")]
public partial class AluePageEdit : ContentPage
{
    private const string ConnectionString = "Server=127.0.0.1;Port=3307;Database=vn;User=root;Password=;SslMode=None;";

    private int? _alueId;
    private string _origNimi = string.Empty;
    private string _origPostinumero = string.Empty;
    private string _origKuvaus = string.Empty;

    private readonly List<PostiItem> _postiItems = new();

    public string AlueId
    {
        set
        {
            if (int.TryParse(value, out var id))
                _alueId = id;
        }
    }

    public AluePageEdit()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadPostiListAsync();

        if (_alueId.HasValue)
        {
            Title = "Muokkaa aluetta";
            await LoadAlueAsync(_alueId.Value);
        }
        else
        {
            Title = "Lisää alue";
            NimiEntry.Text = string.Empty;
            PostiPicker.SelectedIndex = -1;
            KuvausEditor.Text = string.Empty;
        }
    }

    private async Task LoadPostiListAsync()
    {
        try
        {
            _postiItems.Clear();
            PostiPicker.Items.Clear();

            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sql = "SELECT postinro, toimipaikka FROM posti ORDER BY postinro";
            await using var cmd = new MySqlCommand(sql, conn);
            await using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync())
            {
                var postinro = rdr.GetString("postinro");
                var toimipaikka = rdr.IsDBNull(rdr.GetOrdinal("toimipaikka")) ? string.Empty : rdr.GetString("toimipaikka");

                _postiItems.Add(new PostiItem { Postinro = postinro, Toimipaikka = toimipaikka });
                PostiPicker.Items.Add($"{postinro} – {toimipaikka}");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", "Postinumeroiden lataus epäonnistui: " + ex.Message, "OK");
        }
    }

    private void OnPostiPickerChanged(object sender, EventArgs e)
    {
    }

    private async void OnLisaaPostiClicked(object sender, EventArgs e)
    {
        var postinro = await DisplayPromptAsync("Uusi postinumero", "Syötä postinumero:", keyboard: Keyboard.Numeric);
        if (string.IsNullOrWhiteSpace(postinro)) return;

        var toimipaikka = await DisplayPromptAsync("Uusi toimipaikka", "Syötä postitoimipaikka:");
        if (string.IsNullOrWhiteSpace(toimipaikka)) return;

        postinro = postinro.Trim();
        toimipaikka = toimipaikka.Trim();

        try
        {
            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sql = "INSERT INTO posti (postinro, toimipaikka) VALUES (@postinro, @toimipaikka) " +
                               "ON DUPLICATE KEY UPDATE toimipaikka = VALUES(toimipaikka)";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@postinro", postinro);
            cmd.Parameters.AddWithValue("@toimipaikka", toimipaikka);
            await cmd.ExecuteNonQueryAsync();

            await LoadPostiListAsync();
            SelectPostiByPostinro(postinro);

            await DisplayAlert("Valmis", "Postinumero lisätty.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
        }
    }

    private void SelectPostiByPostinro(string postinro)
    {
        for (int i = 0; i < _postiItems.Count; i++)
        {
            if (_postiItems[i].Postinro == postinro)
            {
                PostiPicker.SelectedIndex = i;
                return;
            }
        }
    }

    private PostiItem? GetSelectedPosti()
    {
        var idx = PostiPicker.SelectedIndex;
        if (idx >= 0 && idx < _postiItems.Count)
            return _postiItems[idx];
        return null;
    }

    private async Task LoadAlueAsync(int id)
    {
        try
        {
            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT a.nimi, a.sijainti, a.kuvaus, p.postinro
                FROM alue a
                LEFT JOIN posti p ON p.toimipaikka = a.sijainti
                WHERE a.alue_id = @id";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await using var rdr = await cmd.ExecuteReaderAsync();

            if (await rdr.ReadAsync())
            {
                var nimi = rdr.IsDBNull(rdr.GetOrdinal("nimi")) ? string.Empty : rdr.GetString("nimi");
                var postinro = rdr.IsDBNull(rdr.GetOrdinal("postinro")) ? string.Empty : rdr.GetString("postinro");
                var kuvaus = rdr.IsDBNull(rdr.GetOrdinal("kuvaus")) ? string.Empty : rdr.GetString("kuvaus");

                NimiEntry.Text = nimi;
                KuvausEditor.Text = kuvaus;
                SelectPostiByPostinro(postinro);

                _origNimi = nimi;
                _origPostinumero = postinro;
                _origKuvaus = kuvaus;
            }
            else
            {
                await DisplayAlert("Virhe", "Aluetta ei löytynyt.", "OK");
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _alueId = null;
        await Shell.Current.GoToAsync("..");
    }

    private async void OnSelectImageClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Valitse kuva", FileTypes = FilePickerFileType.Images });
            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
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
        var kuvaus = KuvausEditor.Text?.Trim() ?? string.Empty;

        var selectedPosti = GetSelectedPosti();
        var postinumero = selectedPosti?.Postinro ?? string.Empty;
        var postitoimipaikka = selectedPosti?.Toimipaikka ?? string.Empty;

        if (string.IsNullOrEmpty(nimi))
        {
            await DisplayAlert("Virhe", "Anna nimi.", "OK");
            return;
        }

        // Editing existing record
        if (_alueId.HasValue)
        {
            if (nimi == _origNimi && postinumero == _origPostinumero && kuvaus == _origKuvaus)
            {
                _alueId = null;
                await Shell.Current.GoToAsync("..");
                return;
            }

            try
            {
                await using var conn = new MySqlConnection(ConnectionString);
                await conn.OpenAsync();
                var transaction = await conn.BeginTransactionAsync();
                try
                {
                    const string updateSql = "UPDATE alue SET nimi = @nimi, sijainti = @sijainti, kuvaus = @kuvaus WHERE alue_id = @id";
                    await using var updateCmd = new MySqlCommand(updateSql, conn);
                    updateCmd.Transaction = transaction;
                    updateCmd.Parameters.AddWithValue("@nimi", nimi);
                    updateCmd.Parameters.AddWithValue("@sijainti", postitoimipaikka);
                    updateCmd.Parameters.AddWithValue("@kuvaus", kuvaus);
                    updateCmd.Parameters.AddWithValue("@id", _alueId.Value);
                    await updateCmd.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();
                    await DisplayAlert("Valmis", "Muutokset tallennettu.", "OK");
                    _alueId = null;
                    await Shell.Current.GoToAsync("..");
                }
                catch
                {
                    try { await transaction.RollbackAsync(); } catch { }
                    throw;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
            }
            return;
        }

        // Creating new record
        try
        {
            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();
            var transaction = await conn.BeginTransactionAsync();
            try
            {
                const string alueSql = "INSERT INTO alue (nimi, sijainti, kuvaus) VALUES (@nimi, @sijainti, @kuvaus)";
                await using var alueCmd = new MySqlCommand(alueSql, conn);
                alueCmd.Transaction = transaction;
                alueCmd.Parameters.AddWithValue("@nimi", nimi);
                alueCmd.Parameters.AddWithValue("@sijainti", postitoimipaikka);
                alueCmd.Parameters.AddWithValue("@kuvaus", kuvaus);
                await alueCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                await DisplayAlert("Valmis", "Alue tallennettu.", "OK");
                await Shell.Current.GoToAsync("..");
            }
            catch
            {
                try { await transaction.RollbackAsync(); } catch { }
                throw;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
        }
    }

    private class PostiItem
    {
        public string Postinro { get; set; } = string.Empty;
        public string Toimipaikka { get; set; } = string.Empty;
    }
}
