using MySqlConnector;
using System;
using System.Collections.Generic;

namespace RYHMÄROJEKTI.views;

[QueryProperty(nameof(LaskuId), "laskuId")]
public partial class LaskuPageEdit : ContentPage
{
    private const string ConnectionString = "Server=127.0.0.1;Port=3306;Database=vn;User=root;Password=YES123;SslMode=None;";

    private int? _laskuId;
    private int? _origVarausId;
    private double _origSumma;
    private double _origAlv;
    private double _origMaksettu;

    private readonly List<VarausItem> _varausItems = new();

    public string LaskuId
    {
        set
        {
            if (int.TryParse(value, out var id))
                _laskuId = id;
        }
    }

    public LaskuPageEdit()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadVarausListAsync();

        if (_laskuId.HasValue)
        {
            Title = "Muokkaa laskua";
            await LoadLaskuAsync(_laskuId.Value);
        }
        else
        {
            Title = "Lisää lasku";
            VarausPicker.SelectedIndex = -1;
            SummaEntry.Text = string.Empty;
            AlvEntry.Text = "24";
            MaksettuEntry.Text = "0";
        }
    }

    // ── Varaus picker ───────────────────────────────────────────────────
    private async Task LoadVarausListAsync()
    {
        try
        {
            _varausItems.Clear();
            VarausPicker.Items.Clear();

            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT v.varaus_id, v.asiakas_id, v.mokki_id,
                       v.varattu_alkupvm, v.varattu_loppupvm,
                       a.etunimi, a.sukunimi,
                       m.mokkinimi
                FROM varaus v
                LEFT JOIN asiakas a ON a.asiakas_id = v.asiakas_id
                LEFT JOIN mokki m ON m.mokki_id = v.mokki_id
                ORDER BY v.varaus_id DESC";
            await using var cmd = new MySqlCommand(sql, conn);
            await using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync())
            {
                var varausId = rdr.GetInt32("varaus_id");
                var etunimi = rdr.IsDBNull(rdr.GetOrdinal("etunimi")) ? string.Empty : rdr.GetString("etunimi");
                var sukunimi = rdr.IsDBNull(rdr.GetOrdinal("sukunimi")) ? string.Empty : rdr.GetString("sukunimi");
                var mokkinimi = rdr.IsDBNull(rdr.GetOrdinal("mokkinimi")) ? string.Empty : rdr.GetString("mokkinimi");
                var alkupvm = rdr.IsDBNull(rdr.GetOrdinal("varattu_alkupvm")) ? string.Empty : rdr.GetDateTime("varattu_alkupvm").ToString("dd.MM.yyyy");
                var loppupvm = rdr.IsDBNull(rdr.GetOrdinal("varattu_loppupvm")) ? string.Empty : rdr.GetDateTime("varattu_loppupvm").ToString("dd.MM.yyyy");

                var item = new VarausItem
                {
                    VarausId = varausId,
                    Display = $"#{varausId} – {etunimi} {sukunimi} – {mokkinimi} ({alkupvm}–{loppupvm})"
                };

                _varausItems.Add(item);
                VarausPicker.Items.Add(item.Display);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", "Varausten lataus epäonnistui: " + ex.Message, "OK");
        }
    }

    private void SelectVarausById(int varausId)
    {
        for (int i = 0; i < _varausItems.Count; i++)
        {
            if (_varausItems[i].VarausId == varausId)
            {
                VarausPicker.SelectedIndex = i;
                return;
            }
        }
    }

    private VarausItem? GetSelectedVaraus()
    {
        var idx = VarausPicker.SelectedIndex;
        if (idx >= 0 && idx < _varausItems.Count)
            return _varausItems[idx];
        return null;
    }

    // ── Load existing lasku ─────────────────────────────────────────────
    private async Task LoadLaskuAsync(int id)
    {
        try
        {
            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT varaus_id, summa, alv, maksettu
                FROM lasku
                WHERE lasku_id = @id";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await using var rdr = await cmd.ExecuteReaderAsync();

            if (await rdr.ReadAsync())
            {
                var varausId = rdr.IsDBNull(rdr.GetOrdinal("varaus_id")) ? 0 : rdr.GetInt32("varaus_id");
                var summa = rdr.IsDBNull(rdr.GetOrdinal("summa")) ? 0.0 : rdr.GetDouble("summa");
                var alv = rdr.IsDBNull(rdr.GetOrdinal("alv")) ? 0.0 : rdr.GetDouble("alv");
                var maksettu = rdr.IsDBNull(rdr.GetOrdinal("maksettu")) ? 0.0 : rdr.GetDouble("maksettu");

                SelectVarausById(varausId);
                SummaEntry.Text = summa.ToString("0.##");
                AlvEntry.Text = alv.ToString("0.##");
                MaksettuEntry.Text = maksettu.ToString("0.##");

                _origVarausId = varausId;
                _origSumma = summa;
                _origAlv = alv;
                _origMaksettu = maksettu;
            }
            else
            {
                await DisplayAlert("Virhe", "Laskua ei löytynyt.", "OK");
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
        _laskuId = null;
        await Shell.Current.GoToAsync("..");
    }

    // ── Save ────────────────────────────────────────────────────────────
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var selectedVaraus = GetSelectedVaraus();
        if (selectedVaraus == null)
        {
            await DisplayAlert("Virhe", "Valitse varaus.", "OK");
            return;
        }

        var varausId = selectedVaraus.VarausId;

        if (!double.TryParse(SummaEntry.Text?.Trim(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var summa))
        {
            if (!double.TryParse(SummaEntry.Text?.Trim(), out summa))
                summa = 0;
        }

        if (!double.TryParse(AlvEntry.Text?.Trim(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var alv))
        {
            if (!double.TryParse(AlvEntry.Text?.Trim(), out alv))
                alv = 0;
        }

        if (!double.TryParse(MaksettuEntry.Text?.Trim(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var maksettu))
        {
            if (!double.TryParse(MaksettuEntry.Text?.Trim(), out maksettu))
                maksettu = 0;
        }

        // ── Editing ─────────────────────────────────────────────────────
        if (_laskuId.HasValue)
        {
            if (varausId == _origVarausId && summa == _origSumma
                && alv == _origAlv && maksettu == _origMaksettu)
            {
                _laskuId = null;
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
                        UPDATE lasku
                        SET varaus_id = @varausId, summa = @summa,
                            alv = @alv, maksettu = @maksettu
                        WHERE lasku_id = @id";
                    await using var cmd = new MySqlCommand(updateSql, conn);
                    cmd.Transaction = transaction;
                    cmd.Parameters.AddWithValue("@varausId", varausId);
                    cmd.Parameters.AddWithValue("@summa", summa);
                    cmd.Parameters.AddWithValue("@alv", alv);
                    cmd.Parameters.AddWithValue("@maksettu", maksettu);
                    cmd.Parameters.AddWithValue("@id", _laskuId.Value);
                    await cmd.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();
                    await DisplayAlert("Valmis", "Muutokset tallennettu.", "OK");
                    _laskuId = null;
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
                    INSERT INTO lasku (varaus_id, summa, alv, maksettu)
                    VALUES (@varausId, @summa, @alv, @maksettu)";
                await using var cmd = new MySqlCommand(insertSql, conn);
                cmd.Transaction = transaction;
                cmd.Parameters.AddWithValue("@varausId", varausId);
                cmd.Parameters.AddWithValue("@summa", summa);
                cmd.Parameters.AddWithValue("@alv", alv);
                cmd.Parameters.AddWithValue("@maksettu", maksettu);
                await cmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                await DisplayAlert("Valmis", "Lasku tallennettu.", "OK");
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

    private class VarausItem
    {
        public int VarausId { get; set; }
        public string Display { get; set; } = string.Empty;
    }
}
