using SQLite;
namespace RYHMÄROJEKTI.Models
{
    public class Asiakas
    {
        [PrimaryKey, AutoIncrement]
        public int AsiakasId { get; set; }
        public string Etunimi { get; set; }
        public string Sukunimi { get; set; }
        public string Lahiosoite { get; set; }
        public string Postinumero { get; set; }
        public string Postitoimipaikka { get; set; }
        public string Sahkoposti { get; set; }
        public string Puhelinnro { get; set; }
    }
}