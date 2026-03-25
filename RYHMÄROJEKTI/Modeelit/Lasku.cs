using SQLite;
namespace RYHMÄROJEKTI.Models
{
    public class Lasku
    {
        [PrimaryKey, AutoIncrement]
        public int LaskuId { get; set; }
        public int VarausId { get; set; }
        public double Summa { get; set; }
        public double Alv { get; set; }
        public bool Maksettu { get; set; }
    }
}