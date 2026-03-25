using MySqlConnector;
using RYHMÄROJEKTI.Models;
using System.Data;

namespace RYHMÄROJEKTI.Services
{
    public class DatabaseService
    {
        // MUUTA TÄHÄN OMAT TIETOSI (XAMPP oletus: user=root, password="")
        private string _connectionString = "Server=localhost;Database=villagenewbies;User ID=root;Password=;";

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
    }
}