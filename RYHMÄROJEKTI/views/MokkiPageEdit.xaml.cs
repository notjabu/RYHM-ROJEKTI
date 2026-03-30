using MySqlConnector;
using System;
using System.Collections.Generic;

namespace RYHMÄROJEKTI.views;

[QueryProperty(nameof(MokkiId), "mokkiId")]
public partial class MokkiPageEdit : ContentPage
{
    private const string ConnectionString = "Server=127.0.0.1;Port=3307;Database=vn;User=root;Password=;SslMode=None;";

    private int? _mokkiId;
    private string _origNimi = string.Empty;
    private string _origKatuosoite = string.Empty;
    private string _origPostinro = string.Empty;
    private double _origHinta;
    private int _origHenkilomaara;
    private string _origVarustelu = string.Empty;
    private string _origKuvaus = string.Empty;

    private readonly List<PostiItem> _postiItems = new();

    public string MokkiId
    {
        set
        {
            if (int.TryParse(value, out var id))
                _mokkiId = id;
        }
    }

    public MokkiPageEdit()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadPostiListAsync();

        if (_mokkiId.HasValue)
        {
            Title = "Muokkaa mökkiä";
            await LoadMokkiAsync(_mokkiId.Value);
        }
        else
        {
            Title = "Lisää mökki";
            NimiEntry.Text = string.Empty;
            KatuosoiteEntry.Text = string.Empty;
            PostiPicker.SelectedIndex = -1;
            HintaEntry.Text = string.Empty;
            HenkilomaaraEntry.Text = string.Empty;
            VarusteluEditor.Text = string.Empty;
            KuvausEditor.Text = string.Empty;
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

    // ── Load existing mokki ─────────────────────────────────────────────
    private async Task LoadMokkiAsync(int id)
    {
        try
        {
            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT mokkinimi, katuosoite, postinro, hinta,
                       henkilomaara, varustelu, kuvaus
                FROM mokki
                WHERE mokki_id = @id";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await using var rdr = await cmd.ExecuteReaderAsync();

            if (await rdr.ReadAsync())
            {
                var nimi = rdr.IsDBNull(rdr.GetOrdinal("mokkinimi")) ? string.Empty : rdr.GetString("mokkinimi");
                var katu = rdr.IsDBNull(rdr.GetOrdinal("katuosoite")) ? string.Empty : rdr.GetString("katuosoite");
                var postinro = rdr.IsDBNull(rdr.GetOrdinal("postinro")) ? string.Empty : rdr.GetString("postinro");
                var hinta = rdr.IsDBNull(rdr.GetOrdinal("hinta")) ? 0.0 : rdr.GetDouble("hinta");
                var henkilomaara = rdr.IsDBNull(rdr.GetOrdinal("henkilomaara")) ? 0 : rdr.GetInt32("henkilomaara");
                var varustelu = rdr.IsDBNull(rdr.GetOrdinal("varustelu")) ? string.Empty : rdr.GetString("varustelu");
                var kuvaus = rdr.IsDBNull(rdr.GetOrdinal("kuvaus")) ? string.Empty : rdr.GetString("kuvaus");

                NimiEntry.Text = nimi;
                KatuosoiteEntry.Text = katu;
                SelectPostiByPostinro(postinro);
                HintaEntry.Text = hinta.ToString("0.##");
                HenkilomaaraEntry.Text = henkilomaara.ToString();
                VarusteluEditor.Text = varustelu;
                KuvausEditor.Text = kuvaus;

                _origNimi = nimi;
                _origKatuosoite = katu;
                _origPostinro = postinro;
                _origHinta = hinta;
                _origHenkilomaara = henkilomaara;
                _origVarustelu = varustelu;
                _origKuvaus = kuvaus;
            }
            else
            {
                await DisplayAlert("Virhe", "Mökkiä ei löytynyt.", "OK");
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
        _mokkiId = null;
        await Shell.Current.GoToAsync("..");
    }

    // ── Save ────────────────────────────────────────────────────────────
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var nimi = NimiEntry.Text?.Trim() ?? string.Empty;
        var katuosoite = KatuosoiteEntry.Text?.Trim() ?? string.Empty;
        var kuvaus = KuvausEditor.Text?.Trim() ?? string.Empty;
        var varustelu = VarusteluEditor.Text?.Trim() ?? string.Empty;

        var selectedPosti = GetSelectedPosti();
        var postinro = selectedPosti?.Postinro ?? string.Empty;

        if (!double.TryParse(HintaEntry.Text?.Trim(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var hinta))
        {
            // Try with current culture as fallback (comma decimal separator)
            if (!double.TryParse(HintaEntry.Text?.Trim(), out hinta))
                hinta = 0;
        }

        if (!int.TryParse(HenkilomaaraEntry.Text?.Trim(), out var henkilomaara))
            henkilomaara = 0;

        if (string.IsNullOrEmpty(nimi))
        {
            await DisplayAlert("Virhe", "Anna nimi.", "OK");
            return;
        }

        // ── Editing ─────────────────────────────────────────────────────
        if (_mokkiId.HasValue)
        {
            if (nimi == _origNimi && katuosoite == _origKatuosoite && postinro == _origPostinro
                && hinta == _origHinta && henkilomaara == _origHenkilomaara
                && varustelu == _origVarustelu && kuvaus == _origKuvaus)
            {
                _mokkiId = null;
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
                        UPDATE mokki
                        SET mokkinimi = @nimi, katuosoite = @katu, postinro = @postinro,
                            hinta = @hinta, henkilomaara = @henkilomaara,
                            varustelu = @varustelu, kuvaus = @kuvaus,
                            alue_id = (SELECT alue_id FROM alue WHERE sijainti = (SELECT toimipaikka FROM posti WHERE postinro = @postinro) LIMIT 1)
                        WHERE mokki_id = @id";
                    await using var cmd = new MySqlCommand(updateSql, conn);
                    cmd.Transaction = transaction;
                    cmd.Parameters.AddWithValue("@nimi", nimi);
                    cmd.Parameters.AddWithValue("@katu", katuosoite);
                    cmd.Parameters.AddWithValue("@postinro", postinro);
                    cmd.Parameters.AddWithValue("@hinta", hinta);
                    cmd.Parameters.AddWithValue("@henkilomaara", henkilomaara);
                    cmd.Parameters.AddWithValue("@varustelu", varustelu);
                    cmd.Parameters.AddWithValue("@kuvaus", kuvaus);
                    cmd.Parameters.AddWithValue("@id", _mokkiId.Value);
                    await cmd.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();
                    await DisplayAlert("Valmis", "Muutokset tallennettu.", "OK");
                    _mokkiId = null;
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
                    INSERT INTO mokki (alue_id, mokkinimi, katuosoite, postinro, hinta,
                                      henkilomaara, varustelu, kuvaus)
                    VALUES ((SELECT alue_id FROM alue WHERE sijainti = (SELECT toimipaikka FROM posti WHERE postinro = @postinro) LIMIT 1),
                            @nimi, @katu, @postinro, @hinta,
                            @henkilomaara, @varustelu, @kuvaus)";
                await using var cmd = new MySqlCommand(insertSql, conn);
                cmd.Transaction = transaction;
                cmd.Parameters.AddWithValue("@nimi", nimi);
                cmd.Parameters.AddWithValue("@katu", katuosoite);
                cmd.Parameters.AddWithValue("@postinro", postinro);
                cmd.Parameters.AddWithValue("@hinta", hinta);
                cmd.Parameters.AddWithValue("@henkilomaara", henkilomaara);
                cmd.Parameters.AddWithValue("@varustelu", varustelu);
                cmd.Parameters.AddWithValue("@kuvaus", kuvaus);
                await cmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                await DisplayAlert("Valmis", "Mökki tallennettu.", "OK");
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
