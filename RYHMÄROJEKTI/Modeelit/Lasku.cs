using SQLite;
namespace RYHMÄROJEKTI.Modeelit;

public class Lasku
{
    [PrimaryKey, AutoIncrement]
    public string AsiakasNimi { get; set; }
    public string MokkiNimi { get; set; }
    public string Laskutustapa { get; set; }
    public int LaskuId { get; set; }
    public int VarausId { get; set; }
    public double Summa { get; set; }
    public double Alv { get; set; }
    public bool Maksettu { get; set; }
}