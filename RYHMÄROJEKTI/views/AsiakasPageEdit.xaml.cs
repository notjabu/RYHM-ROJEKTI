using MySqlConnector;
using System;
using System.Collections.Generic;

namespace RYHMÄROJEKTI.views;

[QueryProperty(nameof(AsiakasId), "asiakasId")]
public partial class AsiakasPageEdit : ContentPage
{
    private const string ConnectionString = "Server=127.0.0.1;Port=3306;Database=vn;User=root;Password=YES123;SslMode=None;";

    private int? _asiakasId;
    private string _origEtunimi = string.Empty;
    private string _origSukunimi = string.Empty;
    private string _origLahiosoite = string.Empty;
    private string _origPostinro = string.Empty;
    private string _origEmail = string.Empty;
    private string _origPuhelinnro = string.Empty;

    private readonly List<PostiItem> _postiItems = new();

    public string AsiakasId
    {
        set
        {
            if (int.TryParse(value, out var id))
                _asiakasId = id;
        }
    }

    public AsiakasPageEdit()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadPostiListAsync();

        if (_asiakasId.HasValue)
        {
            Title = "Muokkaa asiakasta";
            await LoadAsiakasAsync(_asiakasId.Value);
        }
        else
        {
            Title = "Lisää asiakas";
            EtunimiEntry.Text = string.Empty;
            SukunimiEntry.Text = string.Empty;
            LahiosoiteEntry.Text = string.Empty;
            PostiPicker.SelectedIndex = -1;
            EmailEntry.Text = string.Empty;
            PuhelinnroEntry.Text = string.Empty;
        }
    }

    // ── Posti picker ────────────────────────────────────────────────────
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

    private void OnPostiPickerChanged(object sender, EventArgs e) { }

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

    // ── Load existing asiakas ───────────────────────────────────────────
    private async Task LoadAsiakasAsync(int id)
    {
        try
        {
            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT etunimi, sukunimi, lahiosoite, postinro,
                       email, puhelinnro
                FROM asiakas
                WHERE asiakas_id = @id";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await using var rdr = await cmd.ExecuteReaderAsync();

            if (await rdr.ReadAsync())
            {
                var etunimi = rdr.IsDBNull(rdr.GetOrdinal("etunimi")) ? string.Empty : rdr.GetString("etunimi");
                var sukunimi = rdr.IsDBNull(rdr.GetOrdinal("sukunimi")) ? string.Empty : rdr.GetString("sukunimi");
                var lahiosoite = rdr.IsDBNull(rdr.GetOrdinal("lahiosoite")) ? string.Empty : rdr.GetString("lahiosoite");
                var postinro = rdr.IsDBNull(rdr.GetOrdinal("postinro")) ? string.Empty : rdr.GetString("postinro");
                var email = rdr.IsDBNull(rdr.GetOrdinal("email")) ? string.Empty : rdr.GetString("email");
                var puhelinnro = rdr.IsDBNull(rdr.GetOrdinal("puhelinnro")) ? string.Empty : rdr.GetString("puhelinnro");

                EtunimiEntry.Text = etunimi;
                SukunimiEntry.Text = sukunimi;
                LahiosoiteEntry.Text = lahiosoite;
                SelectPostiByPostinro(postinro);
                EmailEntry.Text = email;
                PuhelinnroEntry.Text = puhelinnro;

                _origEtunimi = etunimi;
                _origSukunimi = sukunimi;
                _origLahiosoite = lahiosoite;
                _origPostinro = postinro;
                _origEmail = email;
                _origPuhelinnro = puhelinnro;
            }
            else
            {
                await DisplayAlert("Virhe", "Asiakasta ei löytynyt.", "OK");
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    // ── Cancel ──────────────────────────────────────────────────────────
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _asiakasId = null;
        await Shell.Current.GoToAsync("..");
    }

    // ── Save ────────────────────────────────────────────────────────────
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var etunimi = EtunimiEntry.Text?.Trim() ?? string.Empty;
        var sukunimi = SukunimiEntry.Text?.Trim() ?? string.Empty;
        var lahiosoite = LahiosoiteEntry.Text?.Trim() ?? string.Empty;
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var puhelinnro = PuhelinnroEntry.Text?.Trim() ?? string.Empty;

        var selectedPosti = GetSelectedPosti();
        var postinro = selectedPosti?.Postinro ?? string.Empty;

        if (string.IsNullOrEmpty(etunimi) && string.IsNullOrEmpty(sukunimi))
        {
            await DisplayAlert("Virhe", "Anna vähintään etunimi tai sukunimi.", "OK");
            return;
        }

        // ── Editing ─────────────────────────────────────────────────────
        if (_asiakasId.HasValue)
        {
            if (etunimi == _origEtunimi && sukunimi == _origSukunimi && lahiosoite == _origLahiosoite
                && postinro == _origPostinro && email == _origEmail && puhelinnro == _origPuhelinnro)
            {
                _asiakasId = null;
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
                    const string updateSql = @"
                        UPDATE asiakas
                        SET etunimi = @etunimi, sukunimi = @sukunimi,
                            lahiosoite = @lahiosoite, postinro = @postinro,
                            email = @email, puhelinnro = @puhelinnro
                        WHERE asiakas_id = @id";
                    await using var cmd = new MySqlCommand(updateSql, conn);
                    cmd.Transaction = transaction;
                    cmd.Parameters.AddWithValue("@etunimi", etunimi);
                    cmd.Parameters.AddWithValue("@sukunimi", sukunimi);
                    cmd.Parameters.AddWithValue("@lahiosoite", lahiosoite);
                    cmd.Parameters.AddWithValue("@postinro", postinro);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@puhelinnro", puhelinnro);
                    cmd.Parameters.AddWithValue("@id", _asiakasId.Value);
                    await cmd.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();
                    await DisplayAlert("Valmis", "Muutokset tallennettu.", "OK");
                    _asiakasId = null;
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

        // ── Creating new ────────────────────────────────────────────────
        try
        {
            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();
            var transaction = await conn.BeginTransactionAsync();
            try
            {
                const string insertSql = @"
                    INSERT INTO asiakas (etunimi, sukunimi, lahiosoite, postinro,
                                        email, puhelinnro)
                    VALUES (@etunimi, @sukunimi, @lahiosoite, @postinro,
                            @email, @puhelinnro)";
                await using var cmd = new MySqlCommand(insertSql, conn);
                cmd.Transaction = transaction;
                cmd.Parameters.AddWithValue("@etunimi", etunimi);
                cmd.Parameters.AddWithValue("@sukunimi", sukunimi);
                cmd.Parameters.AddWithValue("@lahiosoite", lahiosoite);
                cmd.Parameters.AddWithValue("@postinro", postinro);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@puhelinnro", puhelinnro);
                await cmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                await DisplayAlert("Valmis", "Asiakas tallennettu.", "OK");
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
