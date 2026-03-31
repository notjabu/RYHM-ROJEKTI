using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.PropertyNamingPolicy = null);

var connString = builder.Configuration.GetConnectionString("DefaultConnection")!;

var app = builder.Build();

app.UseHttpsRedirection();

// Helper to open a new SqlConnection from config.
async Task<SqlConnection> OpenDbAsync()
{
    var conn = new SqlConnection(connString);
    await conn.OpenAsync();
    return conn;
}

// SqlDataReader helper methods (SqlDataReader.GetXxx takes int ordinal, not string).
static string Str(SqlDataReader r, string col)
{
    var i = r.GetOrdinal(col);
    return r.IsDBNull(i) ? "" : r.GetString(i);
}
static int? NullInt(SqlDataReader r, string col)
{
    var i = r.GetOrdinal(col);
    return r.IsDBNull(i) ? null : r.GetInt32(i);
}
static int Int(SqlDataReader r, string col)
{
    var i = r.GetOrdinal(col);
    return r.IsDBNull(i) ? 0 : r.GetInt32(i);
}
static double Dbl(SqlDataReader r, string col)
{
    var i = r.GetOrdinal(col);
    return r.IsDBNull(i) ? 0 : (double)r.GetDecimal(i);

}
static DateTime? NullDt(SqlDataReader r, string col)
{
    var i = r.GetOrdinal(col);
    return r.IsDBNull(i) ? null : r.GetDateTime(i);
}

// ─── Posti ──────────────────────────────────────────────────────────────────

app.MapGet("/api/posti", async () =>
{
    await using var conn = await OpenDbAsync();
    const string sql = "SELECT postinro, toimipaikka FROM vn.posti ORDER BY postinro";
    await using var cmd = new SqlCommand(sql, conn);
    await using var rdr = await cmd.ExecuteReaderAsync();

    var list = new List<PostiDto>();
    while (await rdr.ReadAsync())
    {
        list.Add(new PostiDto(
            Str(rdr, "postinro"),
            Str(rdr, "toimipaikka")));
    }
    return Results.Ok(list);
});

app.MapPost("/api/posti", async (PostiDto dto) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        MERGE INTO vn.posti AS target
        USING (SELECT @postinro AS postinro, @toimipaikka AS toimipaikka) AS source
        ON target.postinro = source.postinro
        WHEN MATCHED THEN UPDATE SET toimipaikka = source.toimipaikka
        WHEN NOT MATCHED THEN INSERT (postinro, toimipaikka) VALUES (source.postinro, source.toimipaikka);
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@postinro", dto.Postinro);
    cmd.Parameters.AddWithValue("@toimipaikka", dto.Toimipaikka);
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok();
});

// ─── Alue ───────────────────────────────────────────────────────────────────

app.MapGet("/api/alue", async () =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        SELECT a.alue_id, a.nimi, a.sijainti, a.kuvaus, p.postinro
        FROM vn.alue a
        LEFT JOIN vn.posti p ON p.toimipaikka = a.sijainti
        ORDER BY a.nimi
        """;
    await using var cmd = new SqlCommand(sql, conn);
    await using var rdr = await cmd.ExecuteReaderAsync();

    var list = new List<AlueDto>();
    while (await rdr.ReadAsync())
    {
        list.Add(new AlueDto(
            NullInt(rdr, "alue_id"),
            Str(rdr, "nimi"),
            Str(rdr, "sijainti"),
            Str(rdr, "kuvaus"),
            Str(rdr, "postinro")));
    }
    return Results.Ok(list);
});

app.MapGet("/api/alue/{id:int}", async (int id) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        SELECT a.nimi, a.sijainti, a.kuvaus, p.postinro
        FROM vn.alue a
        LEFT JOIN vn.posti p ON p.toimipaikka = a.sijainti
        WHERE a.alue_id = @id
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    await using var rdr = await cmd.ExecuteReaderAsync();

    if (!await rdr.ReadAsync()) return Results.NotFound();

    return Results.Ok(new AlueDto(
        id,
        Str(rdr, "nimi"),
        Str(rdr, "sijainti"),
        Str(rdr, "kuvaus"),
        Str(rdr, "postinro")));
});

app.MapPost("/api/alue", async (AlueSaveDto dto) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = "INSERT INTO vn.alue (nimi, sijainti, kuvaus) VALUES (@nimi, @sijainti, @kuvaus)";
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@nimi", dto.Nimi);
    cmd.Parameters.AddWithValue("@sijainti", dto.Sijainti);
    cmd.Parameters.AddWithValue("@kuvaus", dto.Kuvaus);
    await cmd.ExecuteNonQueryAsync();
    return Results.Created();
});

app.MapPut("/api/alue/{id:int}", async (int id, AlueSaveDto dto) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = "UPDATE vn.alue SET nimi = @nimi, sijainti = @sijainti, kuvaus = @kuvaus WHERE alue_id = @id";
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@nimi", dto.Nimi);
    cmd.Parameters.AddWithValue("@sijainti", dto.Sijainti);
    cmd.Parameters.AddWithValue("@kuvaus", dto.Kuvaus);
    cmd.Parameters.AddWithValue("@id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0 ? Results.Ok() : Results.NotFound();
});

app.MapDelete("/api/alue/{id:int}", async (int id) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = "DELETE FROM vn.alue WHERE alue_id = @id";
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0 ? Results.Ok() : Results.NotFound();
});

// ─── Mokki ──────────────────────────────────────────────────────────────────

app.MapGet("/api/mokki", async () =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        SELECT m.mokki_id, m.alue_id, m.postinro, m.mokkinimi,
               m.katuosoite, m.hinta, m.kuvaus,
               m.henkilomaara, m.varustelu,
               p.toimipaikka
        FROM vn.mokki m
        LEFT JOIN vn.posti p ON p.postinro = m.postinro
        ORDER BY m.mokkinimi
        """;
    await using var cmd = new SqlCommand(sql, conn);
    await using var rdr = await cmd.ExecuteReaderAsync();

    var list = new List<MokkiDto>();
    while (await rdr.ReadAsync())
    {
        list.Add(new MokkiDto(
            NullInt(rdr, "mokki_id"),
            NullInt(rdr, "alue_id"),
            Str(rdr, "postinro"),
            Str(rdr, "mokkinimi"),
            Str(rdr, "katuosoite"),
            Dbl(rdr, "hinta"),
            Str(rdr, "kuvaus"),
            Int(rdr, "henkilomaara"),
            Str(rdr, "varustelu"),
            Str(rdr, "toimipaikka")));
    }
    return Results.Ok(list);
});

app.MapGet("/api/mokki/{id:int}", async (int id) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        SELECT mokkinimi, katuosoite, postinro, hinta,
               henkilomaara, varustelu, kuvaus
        FROM vn.mokki WHERE mokki_id = @id
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    await using var rdr = await cmd.ExecuteReaderAsync();

    if (!await rdr.ReadAsync()) return Results.NotFound();

    return Results.Ok(new MokkiDto(
        id, null,
        Str(rdr, "postinro"),
        Str(rdr, "mokkinimi"),
        Str(rdr, "katuosoite"),
        Dbl(rdr, "hinta"),
        Str(rdr, "kuvaus"),
        Int(rdr, "henkilomaara"),
        Str(rdr, "varustelu"),
        ""));
});

app.MapPost("/api/mokki", async (MokkiSaveDto dto) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        INSERT INTO vn.mokki (alue_id, mokkinimi, katuosoite, postinro, hinta,
                           henkilomaara, varustelu, kuvaus)
        VALUES ((SELECT TOP 1 alue_id FROM vn.alue WHERE sijainti = (SELECT toimipaikka FROM vn.posti WHERE postinro = @postinro)),
                @nimi, @katu, @postinro, @hinta, @henkilomaara, @varustelu, @kuvaus)
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@nimi", dto.Mokkinimi);
    cmd.Parameters.AddWithValue("@katu", dto.Katuosoite);
    cmd.Parameters.AddWithValue("@postinro", dto.Postinro);
    cmd.Parameters.AddWithValue("@hinta", dto.Hinta);
    cmd.Parameters.AddWithValue("@henkilomaara", dto.Henkilomaara);
    cmd.Parameters.AddWithValue("@varustelu", dto.Varustelu);
    cmd.Parameters.AddWithValue("@kuvaus", dto.Kuvaus);
    await cmd.ExecuteNonQueryAsync();
    return Results.Created();
});

app.MapPut("/api/mokki/{id:int}", async (int id, MokkiSaveDto dto) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        UPDATE vn.mokki
        SET mokkinimi = @nimi, katuosoite = @katu, postinro = @postinro,
            hinta = @hinta, henkilomaara = @henkilomaara,
            varustelu = @varustelu, kuvaus = @kuvaus,
            alue_id = (SELECT TOP 1 alue_id FROM vn.alue WHERE sijainti = (SELECT toimipaikka FROM vn.posti WHERE postinro = @postinro))
        WHERE mokki_id = @id
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@nimi", dto.Mokkinimi);
    cmd.Parameters.AddWithValue("@katu", dto.Katuosoite);
    cmd.Parameters.AddWithValue("@postinro", dto.Postinro);
    cmd.Parameters.AddWithValue("@hinta", dto.Hinta);
    cmd.Parameters.AddWithValue("@henkilomaara", dto.Henkilomaara);
    cmd.Parameters.AddWithValue("@varustelu", dto.Varustelu);
    cmd.Parameters.AddWithValue("@kuvaus", dto.Kuvaus);
    cmd.Parameters.AddWithValue("@id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0 ? Results.Ok() : Results.NotFound();
});

app.MapDelete("/api/mokki/{id:int}", async (int id) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = "DELETE FROM vn.mokki WHERE mokki_id = @id";
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0 ? Results.Ok() : Results.NotFound();
});

// ─── Asiakas ────────────────────────────────────────────────────────────────

app.MapGet("/api/asiakas", async () =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        SELECT a.asiakas_id, a.etunimi, a.sukunimi, a.lahiosoite,
               a.postinro, a.email, a.puhelinnro,
               p.toimipaikka
        FROM vn.asiakas a
        LEFT JOIN vn.posti p ON p.postinro = a.postinro
        ORDER BY a.sukunimi, a.etunimi
        """;
    await using var cmd = new SqlCommand(sql, conn);
    await using var rdr = await cmd.ExecuteReaderAsync();

    var list = new List<AsiakasDto>();
    while (await rdr.ReadAsync())
    {
        list.Add(new AsiakasDto(
            NullInt(rdr, "asiakas_id"),
            Str(rdr, "etunimi"),
            Str(rdr, "sukunimi"),
            Str(rdr, "lahiosoite"),
            Str(rdr, "postinro"),
            Str(rdr, "email"),
            Str(rdr, "puhelinnro"),
            Str(rdr, "toimipaikka")));
    }
    return Results.Ok(list);
});

app.MapGet("/api/asiakas/{id:int}", async (int id) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        SELECT etunimi, sukunimi, lahiosoite, postinro, email, puhelinnro
        FROM vn.asiakas WHERE asiakas_id = @id
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    await using var rdr = await cmd.ExecuteReaderAsync();

    if (!await rdr.ReadAsync()) return Results.NotFound();

    return Results.Ok(new AsiakasDto(
        id,
        Str(rdr, "etunimi"),
        Str(rdr, "sukunimi"),
        Str(rdr, "lahiosoite"),
        Str(rdr, "postinro"),
        Str(rdr, "email"),
        Str(rdr, "puhelinnro"),
        ""));
});

app.MapPost("/api/asiakas", async (AsiakasSaveDto dto) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        INSERT INTO vn.asiakas (etunimi, sukunimi, lahiosoite, postinro, email, puhelinnro)
        VALUES (@etunimi, @sukunimi, @lahiosoite, @postinro, @email, @puhelinnro)
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@etunimi", dto.Etunimi);
    cmd.Parameters.AddWithValue("@sukunimi", dto.Sukunimi);
    cmd.Parameters.AddWithValue("@lahiosoite", dto.Lahiosoite);
    cmd.Parameters.AddWithValue("@postinro", dto.Postinro);
    cmd.Parameters.AddWithValue("@email", dto.Email);
    cmd.Parameters.AddWithValue("@puhelinnro", dto.Puhelinnro);
    await cmd.ExecuteNonQueryAsync();
    return Results.Created();
});

app.MapPut("/api/asiakas/{id:int}", async (int id, AsiakasSaveDto dto) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        UPDATE vn.asiakas
        SET etunimi = @etunimi, sukunimi = @sukunimi,
            lahiosoite = @lahiosoite, postinro = @postinro,
            email = @email, puhelinnro = @puhelinnro
        WHERE asiakas_id = @id
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@etunimi", dto.Etunimi);
    cmd.Parameters.AddWithValue("@sukunimi", dto.Sukunimi);
    cmd.Parameters.AddWithValue("@lahiosoite", dto.Lahiosoite);
    cmd.Parameters.AddWithValue("@postinro", dto.Postinro);
    cmd.Parameters.AddWithValue("@email", dto.Email);
    cmd.Parameters.AddWithValue("@puhelinnro", dto.Puhelinnro);
    cmd.Parameters.AddWithValue("@id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0 ? Results.Ok() : Results.NotFound();
});

app.MapDelete("/api/asiakas/{id:int}", async (int id) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = "DELETE FROM vn.asiakas WHERE asiakas_id = @id";
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0 ? Results.Ok() : Results.NotFound();
});

// ─── Varaus ─────────────────────────────────────────────────────────────────

app.MapGet("/api/varaus", async () =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        SELECT v.varaus_id, v.asiakas_id, v.mokki_id,
               v.varattu_pvm, v.vahvistus_pvm,
               v.varattu_alkupvm, v.varattu_loppupvm,
               a.etunimi, a.sukunimi,
               m.mokkinimi
        FROM vn.varaus v
        LEFT JOIN vn.asiakas a ON a.asiakas_id = v.asiakas_id
        LEFT JOIN vn.mokki m ON m.mokki_id = v.mokki_id
        ORDER BY v.varattu_alkupvm DESC
        """;
    await using var cmd = new SqlCommand(sql, conn);
    await using var rdr = await cmd.ExecuteReaderAsync();

    var list = new List<VarausDto>();
    while (await rdr.ReadAsync())
    {
        list.Add(new VarausDto(
            NullInt(rdr, "varaus_id"),
            NullInt(rdr, "asiakas_id"),
            NullInt(rdr, "mokki_id"),
            Str(rdr, "mokkinimi"),
            Str(rdr, "etunimi"),
            Str(rdr, "sukunimi"),
            NullDt(rdr, "varattu_pvm"),
            NullDt(rdr, "vahvistus_pvm"),
            NullDt(rdr, "varattu_alkupvm"),
            NullDt(rdr, "varattu_loppupvm")));
    }
    return Results.Ok(list);
});

// ─── Lasku ──────────────────────────────────────────────────────────────────

app.MapGet("/api/lasku", async () =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        SELECT l.lasku_id, l.varaus_id, l.summa, l.alv, l.maksettu,
               v.asiakas_id, v.mokki_id, v.varattu_pvm,
               v.vahvistus_pvm, v.varattu_alkupvm, v.varattu_loppupvm,
               a.etunimi, a.sukunimi,
               m.mokkinimi
        FROM vn.lasku l
        LEFT JOIN vn.varaus v ON v.varaus_id = l.varaus_id
        LEFT JOIN vn.asiakas a ON a.asiakas_id = v.asiakas_id
        LEFT JOIN vn.mokki m ON m.mokki_id = v.mokki_id
        ORDER BY l.lasku_id DESC
        """;
    await using var cmd = new SqlCommand(sql, conn);
    await using var rdr = await cmd.ExecuteReaderAsync();

    var list = new List<LaskuDto>();
    while (await rdr.ReadAsync())
    {
        var etunimi = Str(rdr, "etunimi");
        var sukunimi = Str(rdr, "sukunimi");

        list.Add(new LaskuDto(
            NullInt(rdr, "lasku_id"),
            NullInt(rdr, "varaus_id"),
            Dbl(rdr, "summa"),
            Dbl(rdr, "alv"),
            Dbl(rdr, "maksettu"),
            $"{etunimi} {sukunimi}".Trim(),
            Str(rdr, "mokkinimi"),
            NullDt(rdr, "varattu_pvm"),
            NullDt(rdr, "varattu_alkupvm"),
            NullDt(rdr, "varattu_loppupvm")));
    }
    return Results.Ok(list);
});

app.MapGet("/api/lasku/{id:int}", async (int id) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = "SELECT varaus_id, summa, alv, maksettu FROM vn.lasku WHERE lasku_id = @id";
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    await using var rdr = await cmd.ExecuteReaderAsync();

    if (!await rdr.ReadAsync()) return Results.NotFound();

    return Results.Ok(new LaskuDto(
        id,
        NullInt(rdr, "varaus_id"),
        Dbl(rdr, "summa"),
        Dbl(rdr, "alv"),
        Dbl(rdr, "maksettu"),
        "", "", null, null, null));
});

app.MapPost("/api/lasku", async (LaskuSaveDto dto) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        INSERT INTO vn.lasku (varaus_id, summa, alv, maksettu)
        VALUES (@varausId, @summa, @alv, @maksettu)
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@varausId", dto.VarausId);
    cmd.Parameters.AddWithValue("@summa", dto.Summa);
    cmd.Parameters.AddWithValue("@alv", dto.Alv);
    cmd.Parameters.AddWithValue("@maksettu", dto.Maksettu);
    await cmd.ExecuteNonQueryAsync();
    return Results.Created();
});

app.MapPut("/api/lasku/{id:int}", async (int id, LaskuSaveDto dto) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        UPDATE vn.lasku
        SET varaus_id = @varausId, summa = @summa,
            alv = @alv, maksettu = @maksettu
        WHERE lasku_id = @id
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@varausId", dto.VarausId);
    cmd.Parameters.AddWithValue("@summa", dto.Summa);
    cmd.Parameters.AddWithValue("@alv", dto.Alv);
    cmd.Parameters.AddWithValue("@maksettu", dto.Maksettu);
    cmd.Parameters.AddWithValue("@id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0 ? Results.Ok() : Results.NotFound();
});

app.MapDelete("/api/lasku/{id:int}", async (int id) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = "DELETE FROM vn.lasku WHERE lasku_id = @id";
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0 ? Results.Ok() : Results.NotFound();
});

// ─── Tilasto ────────────────────────────────────────────────────────────────

app.MapGet("/api/tilasto", async () =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        SELECT a.nimi AS aluenimi, COUNT(m.mokki_id) AS maara
        FROM vn.alue a
        LEFT JOIN vn.mokki m ON m.alue_id = a.alue_id
        GROUP BY a.alue_id, a.nimi
        ORDER BY a.nimi
        """;
    await using var cmd = new SqlCommand(sql, conn);
    await using var rdr = await cmd.ExecuteReaderAsync();

    var list = new List<TilastoDto>();
    while (await rdr.ReadAsync())
    {
        list.Add(new TilastoDto(Str(rdr, "aluenimi"), Int(rdr, "maara")));
    }
    return Results.Ok(list);
});

app.Run();

// ─── DTOs ───────────────────────────────────────────────────────────────────

record PostiDto(string Postinro, string Toimipaikka);

record AlueDto(int? Id, string Nimi, string Sijainti, string Kuvaus, string Postinumero);
record AlueSaveDto(string Nimi, string Sijainti, string Kuvaus);

record MokkiDto(int? Id, int? AlueId, string Postinro, string Mokkinimi,
    string Katuosoite, double Hinta, string Kuvaus,
    int Henkilomaara, string Varustelu, string Toimipaikka);
record MokkiSaveDto(string Mokkinimi, string Katuosoite, string Postinro,
    double Hinta, int Henkilomaara, string Varustelu, string Kuvaus);

record AsiakasDto(int? Id, string Etunimi, string Sukunimi, string Lahiosoite,
    string Postinro, string Email, string Puhelinnro, string Toimipaikka);
record AsiakasSaveDto(string Etunimi, string Sukunimi, string Lahiosoite,
    string Postinro, string Email, string Puhelinnro);

record VarausDto(int? VarausId, int? AsiakasId, int? MokkiId, string MokkiNimi,
    string Etunimi, string Sukunimi,
    DateTime? VarattuPvm, DateTime? VahvistusPvm,
    DateTime? VarattuAlkuPvm, DateTime? VarattuLoppuPvm);

record LaskuDto(int? Id, int? VarausId, double Summa, double Alv, double Maksettu,
    string AsiakasNimi, string MokkiNimi,
    DateTime? VarattuPvm, DateTime? VarattuAlkuPvm, DateTime? VarattuLoppuPvm);
record LaskuSaveDto(int VarausId, double Summa, double Alv, double Maksettu);

record TilastoDto(string Alue, int Maara);
