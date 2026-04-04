namespace RYHMÄROJEKTI;

public class PostiDto
{
    public string Postinro { get; set; } = "";
    public string Toimipaikka { get; set; } = "";
}

public class AlueDto
{
    public int? Id { get; set; }
    public string Nimi { get; set; } = "";
    public string Sijainti { get; set; } = "";
    public string Kuvaus { get; set; } = "";
    public string Postinumero { get; set; } = "";
}

public class AlueSaveDto
{
    public string Nimi { get; set; } = "";
    public string Sijainti { get; set; } = "";
    public string Kuvaus { get; set; } = "";
}

public class MokkiDto
{
    public int? Id { get; set; }
    public int? AlueId { get; set; }
    public string Postinro { get; set; } = "";
    public string Mokkinimi { get; set; } = "";
    public string Katuosoite { get; set; } = "";
    public double Hinta { get; set; }
    public string Kuvaus { get; set; } = "";
    public int Henkilomaara { get; set; }
    public string Varustelu { get; set; } = "";
    public string Toimipaikka { get; set; } = "";
}

public class MokkiSaveDto
{
    public string Mokkinimi { get; set; } = "";
    public string Katuosoite { get; set; } = "";
    public string Postinro { get; set; } = "";
    public double Hinta { get; set; }
    public int Henkilomaara { get; set; }
    public string Varustelu { get; set; } = "";
    public string Kuvaus { get; set; } = "";
}

public class AsiakasDto
{
    public int? Id { get; set; }
    public string Etunimi { get; set; } = "";
    public string Sukunimi { get; set; } = "";
    public string Lahiosoite { get; set; } = "";
    public string Postinro { get; set; } = "";
    public string Email { get; set; } = "";
    public string Puhelinnro { get; set; } = "";
    public string Toimipaikka { get; set; } = "";
}

public class AsiakasSaveDto
{
    public string Etunimi { get; set; } = "";
    public string Sukunimi { get; set; } = "";
    public string Lahiosoite { get; set; } = "";
    public string Postinro { get; set; } = "";
    public string Email { get; set; } = "";
    public string Puhelinnro { get; set; } = "";
}

public class VarausDto
{
    public int? VarausId { get; set; }
    public int? AsiakasId { get; set; }
    public int? MokkiId { get; set; }
    public string MokkiNimi { get; set; } = "";
    public string Etunimi { get; set; } = "";
    public string Sukunimi { get; set; } = "";
    public DateTime? VarattuPvm { get; set; }
    public DateTime? VahvistusPvm { get; set; }
    public DateTime? VarattuAlkuPvm { get; set; }
    public DateTime? VarattuLoppuPvm { get; set; }
    public List<VarausPalveluDto> Palvelut { get; set; } = new();
}

public class VarausPalveluDto
{
    public int PalveluId { get; set; }
    public string PalveluNimi { get; set; } = "";
    public int Lkm { get; set; }
}

public class LaskuDto
{
    public int? Id { get; set; }
    public int? VarausId { get; set; }
    public double Summa { get; set; }
    public double Alv { get; set; }
    public double Maksettu { get; set; }
    public string AsiakasNimi { get; set; } = "";
    public string MokkiNimi { get; set; } = "";
    public DateTime? VarattuPvm { get; set; }
    public DateTime? VarattuAlkuPvm { get; set; }
    public DateTime? VarattuLoppuPvm { get; set; }
    public int? AsiakasId { get; set; }
}

public class LaskuSaveDto
{
    public int VarausId { get; set; }
    public double Summa { get; set; }
    public double Alv { get; set; }
    public double Maksettu { get; set; }
}

public class PalveluDto
{
    public int? Id { get; set; }
    public int? AlueId { get; set; }
    public string Nimi { get; set; } = "";
    public string Kuvaus { get; set; } = "";
    public double Hinta { get; set; }
    public double Alv { get; set; }
    public string AlueNimi { get; set; } = "";
}

public class PalveluSaveDto
{
    public int? AlueId { get; set; }
    public string Nimi { get; set; } = "";
    public string Kuvaus { get; set; } = "";
    public double Hinta { get; set; }
    public double Alv { get; set; }
}

public class VarausSaveDto
{
    public int AsiakasId { get; set; }
    public int MokkiId { get; set; }
    public DateTime? VahvistusPvm { get; set; }
    public DateTime VarattuAlkuPvm { get; set; }
    public DateTime VarattuLoppuPvm { get; set; }
    public List<VarausPalveluSaveDto> Palvelut { get; set; } = new();
}

public class VarausPalveluSaveDto
{
    public int PalveluId { get; set; }
    public int Lkm { get; set; }
}

public class MokkiVarausDto
{
    public int VarausId { get; set; }
    public DateTime? VarattuAlkuPvm { get; set; }
    public DateTime? VarattuLoppuPvm { get; set; }
}
