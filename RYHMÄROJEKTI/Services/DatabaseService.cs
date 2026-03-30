using MySqlConnector;
using RYHMÄROJEKTI.Modeelit;
using System.Data;


namespace RYHMÄROJEKTI.Services
{
    public class DatabaseService
    {
        // MUUTA TÄHÄN OMAT TIETOSI (XAMPP oletus: user=root, password="")
        private string _connectionString = "Server=127.0.0.1;Port=3307;Database=vn;Uid=projekti;Pwd=salasana123;SslMode=None;";

        public async Task<List<Mokki>> GetMokitAsync()
        {
            var mokit = new List<Mokki>();

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand("SELECT * FROM mokki", connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                mokit.Add(new Mokki
                {
                    Id = reader.GetInt32("mokki_id"),
                    Nimi = reader.GetString("nimi"),
                    Alue = reader.GetString("alue_nimi"),
                    HintaPerPaiva = reader.GetDouble("hinta")
                });
            }
            return mokit;
        }

        public async Task SaveMokkiAsync(Mokki mokki)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "";
            if (mokki.Id == 0)
                query = "INSERT INTO mokki (nimi, alue_nimi, hinta) VALUES (@nimi, @alue, @hinta)";
            else
                query = "UPDATE mokki SET nimi=@nimi, alue_nimi=@alue, hinta=@hinta WHERE mokki_id=@id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@nimi", mokki.Nimi);
            command.Parameters.AddWithValue("@alue", mokki.Alue);
            command.Parameters.AddWithValue("@hinta", mokki.HintaPerPaiva);
            command.Parameters.AddWithValue("@id", mokki.Id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<Alue>> GetAlueetAsync()
        {
            var alueet = new List<Alue>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand("SELECT * FROM alue", connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                alueet.Add(new Alue
                {
                    AlueId = reader.GetInt32("alue_id"),
                    Nimi = reader.GetString("nimi")
                });
            }
            return alueet;
        }
        public async Task SaveAlueAsync(Alue alue)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "";
            if (alue.AlueId == 0)
                query = "INSERT INTO alue (nimi) VALUES (@nimi)";
            else
                query = "UPDATE alue SET nimi=@nimi WHERE alue_id=@id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@nimi", alue.Nimi);
            command.Parameters.AddWithValue("@id", alue.AlueId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAlueAsync(Alue alue)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand("DELETE FROM alue WHERE alue_id=@id", connection);
            command.Parameters.AddWithValue("@id", alue.AlueId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<Lasku>> GetLaskutAsync()
        {
            var laskut = new List<Lasku>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Tämä kysely hakee laskun tiedot ja yhdistää ne varaukseen, asiakkaaseen ja mökkiin
            string query = @"
        SELECT l.*, a.etunimi, a.sukunimi, m.mokkinimi 
        FROM lasku l
        LEFT JOIN varaus v ON l.varaus_id = v.varaus_id
        LEFT JOIN asiakas a ON v.asiakas_id = a.asiakas_id
        LEFT JOIN mokki m ON v.mokki_id = m.mokki_id";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                try
                {
                    laskut.Add(new Lasku
                    {
                      
                        LaskuId = Convert.ToInt32(reader["lasku_id"]),
                        VarausId = Convert.ToInt32(reader["varaus_id"]),
                        Summa = double.Parse(reader["summa"].ToString()),
                        Alv = double.Parse(reader["alv"].ToString()),

                        // TÄMÄ RIVI KORJAA SBYTE-VIRHEEN:
                        Maksettu = Convert.ToBoolean(reader["maksettu"]),

                        Laskutustapa = reader["laskutustapa"]?.ToString() ?? "",
                        AsiakasNimi = $"{reader["etunimi"]} {reader["sukunimi"]}",
                        MokkiNimi = reader["mokkinimi"]?.ToString() ?? ""
                    });
                }
                catch (Exception ex)
                {
                    // Jos jokin rivi kaatuu, näet mikä sarake se oli
                    System.Diagnostics.Debug.WriteLine($"Virhe lukemisessa: {ex.Message}");
                }
            }
            return laskut;
        }
    }
}
