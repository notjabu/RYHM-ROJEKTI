public class LaskuDto
{
    public int? Id { get; set; }
    public int? VarausId { get; set; }
    public double Summa { get; set; }
    public double Alv { get; set; }
    public double Maksettu { get; set; }
    public string AsiakasNimi { get; set; }
    public string MokkiNimi { get; set; }
    public DateTime? VarattuPvm { get; set; }
    public DateTime? VarattuAlkuPvm { get; set; }
    public DateTime? VarattuLoppuPvm { get; set; }
}