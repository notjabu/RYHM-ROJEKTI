namespace RYHMÄROJEKTI.views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using MySqlConnector;

[QueryProperty(nameof(PalveluId), "palveluId")]
public partial class PalveluPageEdit : ContentPage
{
    // Update this connection string to match your database settings
    private const string ConnectionString = "Server=127.0.0.1;Port=3306;Database=vn;User=root;Password=YES123;SslMode=None;";

    // optional: support editing an existing palvelu by query param palveluId
    private int? _palveluId;

    // Areas (alue) for the dropdown
    private readonly ObservableCollection<AreaItem> _areas = new();

    public string PalveluId
    {
        set
        {
            if (int.TryParse(value, out var id))
            {
                _palveluId = id;
                _ = LoadPalveluAsync(id);
            }
        }
    }

    public PalveluPageEdit()
    {
        InitializeComponent();
        // bind picker items from code-behind collection and show 'Nimi' property
        SijaintiPicker.ItemsSource = _areas;
        SijaintiPicker.ItemDisplayBinding = new Binding("Nimi");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Ensure areas are loaded for both "add" and "edit" flows
        await LoadAreasAsync();
    }

    async void PalaaPalveluPageClicked(object sender, EventArgs e)  
    {
        // Use absolute routing for Shell routes (prefix with ///)
        await Shell.Current.GoToAsync("//PalveluPage");
    }

    private async Task<bool> ColumnExistsAsync(MySqlConnection conn, string columnName)
    {
        const string sql = @"
            SELECT COUNT(*) 
            FROM information_schema.columns
            WHERE table_schema = DATABASE() AND table_name = 'palvelu' AND column_name = @col";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@col", columnName);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    // overload: open connection and call existing loader that uses connection
    private async Task LoadAreasAsync()
    {
        try
        {
            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();
            await LoadAreasAsync(conn);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("LoadAreasAsync error: " + ex);
            // swallow because UI should still work; inspect debug output if empty list
        }
    }

    private async Task LoadAreasAsync(MySqlConnection conn)
    {
        _areas.Clear();

        // Try to load official 'alue' table if present
        try
        {
            const string sqlAlue = "SELECT alue_id, nimi FROM alue ORDER BY nimi";
            await using var cmd = new MySqlCommand(sqlAlue, conn);
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                var id = rdr.IsDBNull(0) ? (int?)null : rdr.GetInt32(0);
                var nimi = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1);
                if (id.HasValue && !string.IsNullOrWhiteSpace(nimi))
                    _areas.Add(new AreaItem { Id = id.Value, Nimi = nimi });
            }
        }
        catch
        {
            // If 'alue' table not available, fall back to reading distinct toimipaikka from posti
            try
            {
                const string sqlPosti = "SELECT DISTINCT toimipaikka FROM posti WHERE toimipaikka IS NOT NULL ORDER BY toimipaikka";
                await using var cmd2 = new MySqlCommand(sqlPosti, conn);
                await using var rdr2 = await cmd2.ExecuteReaderAsync();
                int fakeId = 1;
                while (await rdr2.ReadAsync())
                {
                    var t = rdr2.IsDBNull(0) ? string.Empty : rdr2.GetString(0);
                    if (!string.IsNullOrWhiteSpace(t) && !_areas.Any(a => a.Nimi == t))
                        _areas.Add(new AreaItem { Id = fakeId++, Nimi = t });
                }
            }
            catch
            {
                // ignore: leave list empty
            }
        }
    }

    private async Task LoadPalveluAsync(int id)
    {
        try
        {
            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();

            // load possible area options first
            await LoadAreasAsync(conn);

            // detect if 'alue_id' column exists on palvelu
            var hasAlueId = await ColumnExistsAsync(conn, "alue_id");

            string sql = hasAlueId
                ? @"SELECT nimi, alue_id, kuvaus FROM palvelu WHERE palvelu_id = @id LIMIT 1"
                : @"SELECT nimi, kuvaus FROM palvelu WHERE palvelu_id = @id LIMIT 1";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await using var rdr = await cmd.ExecuteReaderAsync();

            if (await rdr.ReadAsync())
            {
                NimiEntry.Text = rdr.IsDBNull(rdr.GetOrdinal("nimi")) ? string.Empty : rdr.GetString("nimi");

                if (hasAlueId)
                {
                    var alueId = rdr.IsDBNull(rdr.GetOrdinal("alue_id")) ? (int?)null : rdr.GetInt32("alue_id");
                    if (alueId.HasValue)
                    {
                        // If the loaded area is not in the list, add a placeholder (attempt to resolve name)
                        var match = _areas.FirstOrDefault(a => a.Id == alueId.Value);
                        if (match != null)
                            SijaintiPicker.SelectedItem = match;
                        else
                        {
                            // add placeholder item
                            var placeholder = new AreaItem { Id = alueId.Value, Nimi = $"Alue #{alueId.Value}" };
                            _areas.Add(placeholder);
                            SijaintiPicker.SelectedItem = placeholder;
                        }
                    }
                    else
                    {
                        SijaintiPicker.SelectedItem = null;
                    }
                }
                else
                {
                    // no alue_id: no DB mapping; leave unselected
                    SijaintiPicker.SelectedItem = null;
                }

                KuvausEditor.Text = rdr.IsDBNull(rdr.GetOrdinal("kuvaus")) ? string.Empty : rdr.GetString("kuvaus");
            }
            else
            {
                await DisplayAlert("Virhe", "Palvelua ei löytynyt.", "OK");
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Tietokantavirhe", ex.Message, "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var nimi = NimiEntry.Text?.Trim() ?? string.Empty;
        var selectedArea = SijaintiPicker.SelectedItem as AreaItem;
        int? alueId = selectedArea?.Id;
        var kuvaus = KuvausEditor.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(nimi))
        {
            await DisplayAlert("Virhe", "Nimi ei voi olla tyhjä.", "OK");
            return;
        }

        try
        {
            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();

            var hasAlueId = await ColumnExistsAsync(conn, "alue_id");

            var transaction = await conn.BeginTransactionAsync();
            try
            {
                if (_palveluId.HasValue)
                {
                    string updateSql = hasAlueId
                        ? @"UPDATE palvelu SET nimi = @nimi, alue_id = @alue_id, kuvaus = @kuvaus WHERE palvelu_id = @id"
                        : @"UPDATE palvelu SET nimi = @nimi, kuvaus = @kuvaus WHERE palvelu_id = @id";

                    await using var cmd = new MySqlCommand(updateSql, conn);
                    cmd.Transaction = transaction;
                    cmd.Parameters.AddWithValue("@nimi", nimi);
                    if (hasAlueId) cmd.Parameters.AddWithValue("@alue_id", alueId.HasValue ? (object)alueId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@kuvaus", kuvaus);
                    cmd.Parameters.AddWithValue("@id", _palveluId.Value);
                    await cmd.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();
                    await DisplayAlert("Valmis", "Muutokset tallennettu.", "OK");
                }
                else
                {
                    string insertSql = hasAlueId
                        ? @"INSERT INTO palvelu (nimi, alue_id, kuvaus) VALUES (@nimi, @alue_id, @kuvaus)"
                        : @"INSERT INTO palvelu (nimi, kuvaus) VALUES (@nimi, @kuvaus)";

                    await using var cmd = new MySqlCommand(insertSql, conn);
                    cmd.Transaction = transaction;
                    cmd.Parameters.AddWithValue("@nimi", nimi);
                    if (hasAlueId) cmd.Parameters.AddWithValue("@alue_id", alueId.HasValue ? (object)alueId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@kuvaus", kuvaus);
                    await cmd.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();
                    await DisplayAlert("Valmis", "Palvelu tallennettu.", "OK");
                }

                _palveluId = null;
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

    // small model for picker items
    class AreaItem
    {
        public int Id { get; set; }
        public string Nimi { get; set; } = string.Empty;
        public override string ToString() => Nimi;
    }
}