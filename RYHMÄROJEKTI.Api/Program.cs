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
        SELECT alue_id, mokkinimi, katuosoite, postinro, hinta,
               henkilomaara, varustelu, kuvaus
        FROM vn.mokki WHERE mokki_id = @id
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    await using var rdr = await cmd.ExecuteReaderAsync();

    if (!await rdr.ReadAsync()) return Results.NotFound();

    return Results.Ok(new MokkiDto(
        id, NullInt(rdr, "alue_id"),
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
        VALUES (@alueId, @nimi, @katu, @postinro, @hinta, @henkilomaara, @varustelu, @kuvaus)
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@alueId", dto.AlueId);
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
            alue_id = @alueId
        WHERE mokki_id = @id
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@alueId", dto.AlueId);
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

app.MapGet("/api/varaus/mokki/{mokkiId:int}", async (int mokkiId) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        SELECT varaus_id, varattu_alkupvm, varattu_loppupvm
        FROM vn.varaus
        WHERE mokki_id = @mokkiId
          AND varattu_loppupvm >= CAST(GETDATE() AS date)
        ORDER BY varattu_alkupvm
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@mokkiId", mokkiId);
    await using var rdr = await cmd.ExecuteReaderAsync();

    var list = new List<object>();
    while (await rdr.ReadAsync())
    {
        list.Add(new
        {
            VarausId = Int(rdr, "varaus_id"),
            VarattuAlkuPvm = NullDt(rdr, "varattu_alkupvm"),
            VarattuLoppuPvm = NullDt(rdr, "varattu_loppupvm")
        });
    }
    return Results.Ok(list);
});

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
    await using var rdr = await cmd.ExecuteReaderAsync();  // First reader opened

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
            NullDt(rdr, "varattu_loppupvm"),
            new List<VarausPalveluDto>()));
    }

    rdr.Close();

    var lookup = list.Where(v => v.VarausId.HasValue)
                        .ToDictionary(v => v.VarausId!.Value);

    if (list.Count > 0)
    {
        const string palveluSql = """
            SELECT vp.varaus_id, vp.palvelu_id, p.nimi, vp.lkm
            FROM vn.varauksen_palvelut vp
            INNER JOIN vn.palvelu p ON p.palvelu_id = vp.palvelu_id
            ORDER BY vp.varaus_id, p.nimi
            """;
        await using var cmd2 = new SqlCommand(palveluSql, conn);
        await using var rdr2 = await cmd2.ExecuteReaderAsync();  // <-- EXCEPTION HERE

        while (await rdr2.ReadAsync())  // <-- BUG: Using rdr instead of rdr2
        {
            var vid = Int(rdr2, "varaus_id");
            if (lookup.TryGetValue(vid, out var varaus))
            {
                varaus.Palvelut.Add(new VarausPalveluDto(
                    Int(rdr2, "palvelu_id"),
                    Str(rdr2, "nimi"),
                    Int(rdr2, "lkm")));
            }
        }
    }

    return Results.Ok(list);
});

app.MapPost("/api/varaus", async (VarausSaveDto dto) =>
{
    await using var conn = await OpenDbAsync();
    await using var tx = conn.BeginTransaction();
    try
    {
        const string insertVaraus = """
            INSERT INTO vn.varaus (asiakas_id, mokki_id, varattu_pvm, vahvistus_pvm, varattu_alkupvm, varattu_loppupvm)
            OUTPUT INSERTED.varaus_id
            VALUES (@asiakasId, @mokkiId, @varattuPvm, @vahvistusPvm, @alkupvm, @loppupvm)
            """;
        await using var cmd = new SqlCommand(insertVaraus, conn, tx);
        cmd.Parameters.AddWithValue("@asiakasId", dto.AsiakasId);
        cmd.Parameters.AddWithValue("@mokkiId", dto.MokkiId);
        cmd.Parameters.AddWithValue("@varattuPvm", DateTime.Now);
        cmd.Parameters.AddWithValue("@vahvistusPvm", (object?)dto.VahvistusPvm ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@alkupvm", dto.VarattuAlkuPvm);
        cmd.Parameters.AddWithValue("@loppupvm", dto.VarattuLoppuPvm);
        var varausId = (int)await cmd.ExecuteScalarAsync();

        foreach (var p in dto.Palvelut)
        {
            const string insertPalvelu = """
                INSERT INTO vn.varauksen_palvelut (varaus_id, palvelu_id, lkm)
                VALUES (@varausId, @palveluId, @lkm)
                """;
            await using var cmd2 = new SqlCommand(insertPalvelu, conn, tx);
            cmd2.Parameters.AddWithValue("@varausId", varausId);
            cmd2.Parameters.AddWithValue("@palveluId", p.PalveluId);
            cmd2.Parameters.AddWithValue("@lkm", p.Lkm);
            await cmd2.ExecuteNonQueryAsync();
        }

        tx.Commit();
        return Results.Created($"/api/varaus/{varausId}", null);
    }
    catch
    {
        tx.Rollback();
        throw;
    }
});

app.MapGet("/api/varaus/{id:int}", async (int id) =>
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
        WHERE v.varaus_id = @id
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    await using var rdr = await cmd.ExecuteReaderAsync();

    if (!await rdr.ReadAsync()) return Results.NotFound();

    var dto = new VarausDto(
        NullInt(rdr, "varaus_id"),
        NullInt(rdr, "asiakas_id"),
        NullInt(rdr, "mokki_id"),
        Str(rdr, "mokkinimi"),
        Str(rdr, "etunimi"),
        Str(rdr, "sukunimi"),
        NullDt(rdr, "varattu_pvm"),
        NullDt(rdr, "vahvistus_pvm"),
        NullDt(rdr, "varattu_alkupvm"),
        NullDt(rdr, "varattu_loppupvm"),
        new List<VarausPalveluDto>());

    rdr.Close();

    const string palveluSql = """
        SELECT vp.palvelu_id, p.nimi, vp.lkm
        FROM vn.varauksen_palvelut vp
        INNER JOIN vn.palvelu p ON p.palvelu_id = vp.palvelu_id
        WHERE vp.varaus_id = @vid
        ORDER BY p.nimi
        """;
    await using var cmd2 = new SqlCommand(palveluSql, conn);
    cmd2.Parameters.AddWithValue("@vid", id);
    await using var rdr2 = await cmd2.ExecuteReaderAsync();

    while (await rdr2.ReadAsync())
    {
        dto.Palvelut.Add(new VarausPalveluDto(
            Int(rdr2, "palvelu_id"),
            Str(rdr2, "nimi"),
            Int(rdr2, "lkm")));
    }

    return Results.Ok(dto);
});

app.MapPut("/api/varaus/{id:int}", async (int id, VarausSaveDto dto) =>
{
    await using var conn = await OpenDbAsync();
    await using var tx = conn.BeginTransaction();
    try
    {
        const string updateSql = """
            UPDATE vn.varaus
            SET asiakas_id = @asiakasId, mokki_id = @mokkiId,
                vahvistus_pvm = @vahvistusPvm,
                varattu_alkupvm = @alkupvm, varattu_loppupvm = @loppupvm
            WHERE varaus_id = @id
            """;
        await using var cmd = new SqlCommand(updateSql, conn, tx);
        cmd.Parameters.AddWithValue("@asiakasId", dto.AsiakasId);
        cmd.Parameters.AddWithValue("@mokkiId", dto.MokkiId);
        cmd.Parameters.AddWithValue("@vahvistusPvm", (object?)dto.VahvistusPvm ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@alkupvm", dto.VarattuAlkuPvm);
        cmd.Parameters.AddWithValue("@loppupvm", dto.VarattuLoppuPvm);
        cmd.Parameters.AddWithValue("@id", id);
        var rows = await cmd.ExecuteNonQueryAsync();
        if (rows == 0) { tx.Rollback(); return Results.NotFound(); }

        const string deletePalvelut = "DELETE FROM vn.varauksen_palvelut WHERE varaus_id = @vid";
        await using var cmdDel = new SqlCommand(deletePalvelut, conn, tx);
        cmdDel.Parameters.AddWithValue("@vid", id);
        await cmdDel.ExecuteNonQueryAsync();

        foreach (var p in dto.Palvelut)
        {
            const string insertPalvelu = """
                INSERT INTO vn.varauksen_palvelut (varaus_id, palvelu_id, lkm)
                VALUES (@varausId, @palveluId, @lkm)
                """;
            await using var cmd2 = new SqlCommand(insertPalvelu, conn, tx);
            cmd2.Parameters.AddWithValue("@varausId", id);
            cmd2.Parameters.AddWithValue("@palveluId", p.PalveluId);
            cmd2.Parameters.AddWithValue("@lkm", p.Lkm);
            await cmd2.ExecuteNonQueryAsync();
        }

        tx.Commit();
        return Results.Ok();
    }
    catch
    {
        tx.Rollback();
        throw;
    }
});

app.MapDelete("/api/varaus/{id:int}", async (int id) =>
{
    await using var conn = await OpenDbAsync();
    await using var tx = conn.BeginTransaction();
    try
    {
        const string deletePalvelut = "DELETE FROM vn.varauksen_palvelut WHERE varaus_id = @vid";
        await using var cmdPal = new SqlCommand(deletePalvelut, conn, tx);
        cmdPal.Parameters.AddWithValue("@vid", id);
        await cmdPal.ExecuteNonQueryAsync();

        const string deleteVaraus = "DELETE FROM vn.varaus WHERE varaus_id = @id";
        await using var cmd = new SqlCommand(deleteVaraus, conn, tx);
        cmd.Parameters.AddWithValue("@id", id);
        var rows = await cmd.ExecuteNonQueryAsync();

        if (rows == 0) { tx.Rollback(); return Results.NotFound(); }

        tx.Commit();
        return Results.Ok();
    }
    catch
    {
        tx.Rollback();
        throw;
    }
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
            NullDt(rdr, "varattu_loppupvm"),
            NullInt(rdr, "asiakas_id")));
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
        "", "", null, null, null, NullInt(rdr, "asiakas_id")));
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

// ─── Palvelu ────────────────────────────────────────────────────────────────

app.MapGet("/api/palvelu", async () =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        SELECT p.palvelu_id, p.alue_id, p.nimi, p.kuvaus, p.hinta, p.alv,
               a.nimi AS aluenimi
        FROM vn.palvelu p
        LEFT JOIN vn.alue a ON a.alue_id = p.alue_id
        ORDER BY p.nimi
        """;
    await using var cmd = new SqlCommand(sql, conn);
    await using var rdr = await cmd.ExecuteReaderAsync();

    var list = new List<PalveluDto>();
    while (await rdr.ReadAsync())
    {
        list.Add(new PalveluDto(
            NullInt(rdr, "palvelu_id"),
            NullInt(rdr, "alue_id"),
            Str(rdr, "nimi"),
            Str(rdr, "kuvaus"),
            Dbl(rdr, "hinta"),
            Dbl(rdr, "alv"),
            Str(rdr, "aluenimi")));
    }
    return Results.Ok(list);
});

app.MapGet("/api/palvelu/{id:int}", async (int id) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = "SELECT alue_id, nimi, kuvaus, hinta, alv FROM vn.palvelu WHERE palvelu_id = @id";
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    await using var rdr = await cmd.ExecuteReaderAsync();

    if (!await rdr.ReadAsync()) return Results.NotFound();

    return Results.Ok(new PalveluDto(
        id,
        NullInt(rdr, "alue_id"),
        Str(rdr, "nimi"),
        Str(rdr, "kuvaus"),
        Dbl(rdr, "hinta"),
        Dbl(rdr, "alv"),
        ""));
});

app.MapPost("/api/palvelu", async (PalveluSaveDto dto) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        INSERT INTO vn.palvelu (alue_id, nimi, kuvaus, hinta, alv)
        VALUES (@alueId, @nimi, @kuvaus, @hinta, @alv)
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@alueId", (object?)dto.AlueId ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@nimi", dto.Nimi);
    cmd.Parameters.AddWithValue("@kuvaus", dto.Kuvaus);
    cmd.Parameters.AddWithValue("@hinta", dto.Hinta);
    cmd.Parameters.AddWithValue("@alv", dto.Alv);
    await cmd.ExecuteNonQueryAsync();
    return Results.Created();
});

app.MapPut("/api/palvelu/{id:int}", async (int id, PalveluSaveDto dto) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = """
        UPDATE vn.palvelu
        SET alue_id = @alueId, nimi = @nimi,
            kuvaus = @kuvaus, hinta = @hinta, alv = @alv
        WHERE palvelu_id = @id
        """;
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@alueId", (object?)dto.AlueId ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@nimi", dto.Nimi);
    cmd.Parameters.AddWithValue("@kuvaus", dto.Kuvaus);
    cmd.Parameters.AddWithValue("@hinta", dto.Hinta);
    cmd.Parameters.AddWithValue("@alv", dto.Alv);
    cmd.Parameters.AddWithValue("@id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0 ? Results.Ok() : Results.NotFound();
});

app.MapDelete("/api/palvelu/{id:int}", async (int id) =>
{
    await using var conn = await OpenDbAsync();
    const string sql = "DELETE FROM vn.palvelu WHERE palvelu_id = @id";
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
        SELECT a.nimi AS aluenimi,
               (SELECT COUNT(*) FROM vn.mokki   m WHERE m.alue_id = a.alue_id)    AS maara,
               (SELECT COUNT(*) FROM vn.palvelu p WHERE p.alue_id = a.alue_id)    AS maara2
        FROM vn.alue a
        ORDER BY a.nimi
        """;
    await using var cmd = new SqlCommand(sql, conn);
    await using var rdr = await cmd.ExecuteReaderAsync();

    var list = new List<TilastoDto>();
    while (await rdr.ReadAsync())
    {
        list.Add(new TilastoDto(Str(rdr, "aluenimi"), Int(rdr, "maara"), Int(rdr,"maara2")));
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
record MokkiSaveDto(int AlueId, string Mokkinimi, string Katuosoite, string Postinro,
    double Hinta, int Henkilomaara, string Varustelu, string Kuvaus);

record AsiakasDto(int? Id, string Etunimi, string Sukunimi, string Lahiosoite,
    string Postinro, string Email, string Puhelinnro, string Toimipaikka);
record AsiakasSaveDto(string Etunimi, string Sukunimi, string Lahiosoite,
    string Postinro, string Email, string Puhelinnro);

record VarausPalveluDto(int PalveluId, string PalveluNimi, int Lkm);

record VarausDto(int? VarausId, int? AsiakasId, int? MokkiId, string MokkiNimi,
    string Etunimi, string Sukunimi,
    DateTime? VarattuPvm, DateTime? VahvistusPvm,
    DateTime? VarattuAlkuPvm, DateTime? VarattuLoppuPvm,
    List<VarausPalveluDto> Palvelut);

record LaskuDto(int? Id, int? VarausId, double Summa, double Alv, double Maksettu,
    string AsiakasNimi, string MokkiNimi,
    DateTime? VarattuPvm, DateTime? VarattuAlkuPvm, DateTime? VarattuLoppuPvm,
    int? AsiakasId);
record LaskuSaveDto(int VarausId, double Summa, double Alv, double Maksettu);

record PalveluDto(int? Id, int? AlueId, string Nimi, string Kuvaus,
    double Hinta, double Alv, string AlueNimi);
record PalveluSaveDto(int? AlueId, string Nimi, string Kuvaus, double Hinta, double Alv);

record VarausSaveDto(int AsiakasId, int MokkiId, DateTime? VahvistusPvm,
    DateTime VarattuAlkuPvm, DateTime VarattuLoppuPvm,
    List<VarausPalveluSaveDto> Palvelut);
record VarausPalveluSaveDto(int PalveluId, int Lkm);

record TilastoDto(string Alue, int Maara, int Maara2);
