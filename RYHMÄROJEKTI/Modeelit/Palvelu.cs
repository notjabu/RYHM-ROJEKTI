using SQLite;
namespace RYHMÄROJEKTI.Models
{
    public class Palvelu
    {
        [PrimaryKey, AutoIncrement]
        public int PalveluId { get; set; }
        public int AlueId { get; set; } // Palvelut on linkitetty 
        public string Nimi { get; set; }
        public string Kuvaus { get; set; }
        public double Hinta { get; set; }
        public double Alv { get; set; }
    }
}