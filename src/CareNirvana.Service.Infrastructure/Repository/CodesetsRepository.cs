using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;


namespace CareNirvana.Service.Infrastructure.Repository
{
    public class CodesetsRepository : ICodesetsRepository
    {
        private readonly string _connectionString;

        public CodesetsRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public async Task<IEnumerable<codesets>> GetAllAsync(string type)
        {
            var list = new List<codesets>();

            using var conn = GetConnection();
            await conn.OpenAsync();

            if (type == "ICD")
            {
                var cmd = new NpgsqlCommand("SELECT * FROM cfgicdcodesmaster LIMIT 20", conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new codesets
                    {
                        Id = reader["icdcodeid"] != DBNull.Value ? Convert.ToInt32(reader["icdcodeid"]) : null,
                        code = reader["code"]?.ToString(),
                        codeDesc = reader["codedescription"]?.ToString(),
                        codeShortDesc = reader["codeshortdescription"]?.ToString(),
                        effectiveDate = reader["effectivedate"] as DateTime?,
                        endDate = reader["enddate"] as DateTime?,
                        severity = reader["severity"]?.ToString(),
                        laterality = reader["laterality"]?.ToString(),
                        activeFlag = reader["activeflag"]?.ToString(),
                        type = "ICD"
                    });
                }
            }
            else
            {
                var cmd = new NpgsqlCommand("SELECT * FROM cfgmedicalcodesmaster LIMIT 20", conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new codesets
                    {
                        Id = reader["medicalcodemasterid"] != DBNull.Value ? Convert.ToInt32(reader["medicalcodemasterid"]) : null,
                        code = reader["code"]?.ToString(),
                        codeDesc = reader["codedescription"]?.ToString(),
                        codeShortDesc = reader["codeshortdescription"]?.ToString(),
                        effectiveDate = reader["effectivedate"] as DateTime?,
                        endDate = reader["enddate"] as DateTime?,
                        activeFlag = reader["activeflag"]?.ToString(),
                        type = "CPT"
                    });
                }
            }

            return list;
        }

        public async Task<codesets?> GetByIdAsync(string id, string type)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            if (type == "ICD")
            {
                var cmd = new NpgsqlCommand("SELECT * FROM cfgicdcodesmaster WHERE code = @id", conn);
                cmd.Parameters.AddWithValue("id", id);

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new codesets
                    {
                        Id = reader["icdcodeid"] != DBNull.Value ? Convert.ToInt32(reader["icdcodeid"]) : null,
                        code = reader["code"]?.ToString(),
                        codeDesc = reader["codedescription"]?.ToString(),
                        codeShortDesc = reader["codeshortdescription"]?.ToString(),
                        effectiveDate = reader["effectivedate"] as DateTime?,
                        endDate = reader["enddate"] as DateTime?,
                        severity = reader["severity"]?.ToString(),
                        laterality = reader["laterality"]?.ToString(),
                        activeFlag = reader["activeflag"]?.ToString(),
                        type = "ICD"
                    };
                }
            }
            else
            {
                var cmd = new NpgsqlCommand("SELECT * FROM cfgmedicalcodesmaster WHERE code = @id", conn);
                cmd.Parameters.AddWithValue("id", id);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    return new codesets
                    {
                        Id = reader["medicalcodemasterid"] != DBNull.Value ? Convert.ToInt32(reader["medicalcodemasterid"]) : null,
                        code = reader["code"]?.ToString(),
                        codeDesc = reader["codedescription"]?.ToString(),
                        codeShortDesc = reader["codeshortdescription"]?.ToString(),
                        effectiveDate = reader["effectivedate"] as DateTime?,
                        endDate = reader["enddate"] as DateTime?,
                        activeFlag = reader["activeflag"]?.ToString(),
                        type = "CPT"
                    };
                }
            }

            return null;
        }

        public async Task<codesets> InsertAsync(codesets entity)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
            INSERT INTO cfgicdcodesmaster 
            (code, codedescription, codeshortdescription, effectivedate, enddate, severity, laterality, activeflag) 
            VALUES 
            (@code, @codedesc, @codeshortdesc, @effectivedate, @enddate, @severity, @laterality, @activeflag)
            RETURNING id", conn);

            cmd.Parameters.AddWithValue("code", entity.code ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("codedescription", entity.codeDesc ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("codeshortdescription", entity.codeShortDesc ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("effectivedate", (object?)entity.effectiveDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("enddate", (object?)entity.endDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("severity", entity.severity ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("laterality", entity.laterality ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("activeflag", entity.activeFlag ?? (object)DBNull.Value);

            entity.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return entity;
        }

        public async Task<codesets> UpdateAsync(codesets entity)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
            UPDATE cfgicdcodesmaster SET 
                code = @code,
                codedescription = @codedesc,
                codeshortdescription = @codeshortdesc,
                effectivedate = @effectivedate,
                enddate = @enddate,
                severity = @severity,
                laterality = @laterality,
                activeflag = @activeflag
            WHERE icdcodeid = @id", conn);

            cmd.Parameters.AddWithValue("id", entity.Id ?? throw new ArgumentNullException(nameof(entity.Id)));
            cmd.Parameters.AddWithValue("code", entity.code ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("codedescription", entity.codeDesc ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("codeshortdescription", entity.codeShortDesc ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("effectivedate", (object?)entity.effectiveDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("enddate", (object?)entity.endDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("severity", entity.severity ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("laterality", entity.laterality ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("activeflag", entity.activeFlag ?? (object)DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
            return entity;
        }

        public async Task<IReadOnlyList<CodeSearchResult>> SearchIcdAsync(
            string q,
            int limit = 25,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Array.Empty<CodeSearchResult>();

            q = q.Trim();

            await using var conn = GetConnection();
            await conn.OpenAsync(ct);

            const string sql = @"
                SELECT icdcodeid, code, codedescription, codeshortdescription
                FROM cfgicdcodesmaster
                WHERE (activeflag IS NULL OR activeflag <> 'N')
                  AND (
                        code ILIKE @starts
                     OR codedescription ILIKE @contains
                     OR codeshortdescription ILIKE @contains
                  )
                ORDER BY
                    CASE WHEN code ILIKE @starts THEN 0 ELSE 1 END,
                    LENGTH(code),
                    code
                LIMIT @limit;";

            await using var cmd = new NpgsqlCommand(sql, conn);

            // ✅ consistent parameter names
            cmd.Parameters.AddWithValue("starts", $"{q}%");
            cmd.Parameters.AddWithValue("contains", $"%{q}%");
            cmd.Parameters.AddWithValue("limit", limit);

            var results = new List<CodeSearchResult>(limit);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new CodeSearchResult
                {
                    Id = reader["icdcodeid"] != DBNull.Value ? Convert.ToInt32(reader["icdcodeid"]) : null,
                    code = reader["code"]?.ToString(),
                    codeDesc = reader["codedescription"]?.ToString(),
                    codeShortDesc = reader["codeshortdescription"]?.ToString(),
                    type = "ICD"
                });
            }

            return results;
        }

        public async Task<IReadOnlyList<CodeSearchResult>> SearchMedicalCodesAsync(
            string q,
            int limit = 25,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Array.Empty<CodeSearchResult>();

            q = q.Trim();

            await using var conn = GetConnection();
            await conn.OpenAsync(ct);

            const string sql = @"
                SELECT medicalcodemasterid, code, codedescription, codeshortdescription
                FROM cfgmedicalcodesmaster
                WHERE (activeflag IS NULL OR activeflag <> 'N')
                  AND (
                        code ILIKE @starts
                     OR codedescription ILIKE @contains
                     OR codeshortdescription ILIKE @contains
                  )
                ORDER BY
                    CASE WHEN code ILIKE @starts THEN 0 ELSE 1 END,
                    LENGTH(code),
                    code
                LIMIT @limit;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("starts", $"{q}%");
            cmd.Parameters.AddWithValue("contains", $"%{q}%");
            cmd.Parameters.AddWithValue("limit", limit);

            var results = new List<CodeSearchResult>(limit);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new CodeSearchResult
                {
                    Id = reader["medicalcodemasterid"] != DBNull.Value ? Convert.ToInt32(reader["medicalcodemasterid"]) : null,
                    code = reader["code"]?.ToString(),
                    codeDesc = reader["codedescription"]?.ToString(),
                    codeShortDesc = reader["codeshortdescription"]?.ToString(),
                    type = "CPT"
                });
            }

            return results;
        }

        public async Task<IReadOnlyList<MemberSearchResult>> SearchMembersAsync(
            string q,
            int limit = 25,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Array.Empty<MemberSearchResult>();

            q = q.Trim();
            var qDigits = new string(q.Where(char.IsDigit).ToArray()); // for phone search

            await using var conn = GetConnection();
            await conn.OpenAsync(ct);

            const string sql = @"
                select
                    md.firstname,
                    md.lastname,
                    md.memberid,
                    md.memberdetailsid,
                    to_char(md.birthdate::date, 'MM-DD-YYYY') as birthdate,
                    ma.city,
                    mp.phone,
                    gen.gender
                from memberdetails md

                -- ✅ pick ONE primary address row (if multiple)
                left join lateral (
                    select a.city
                    from memberaddress a
                    where a.memberdetailsid = md.memberdetailsid
                      and a.isprimary = true
                    order by a.memberaddressid desc
                    limit 1
                ) ma on true

                -- ✅ pick ONE preferred phone row (if multiple)
                left join lateral (
                    select p.phonenumber::text as phone
                    from memberphonenumber p
                    where p.memberdetailsid = md.memberdetailsid
                      and p.ispreferred = true
                    order by p.memberphonenumberid desc
                    limit 1
                ) mp on true

                left join lateral (
                    select elem->>'gender' as gender
                    from cfgadmindata cad,
                         jsonb_array_elements(cad.jsoncontent::jsonb->'gender') elem
                    where (elem->>'id')::int = md.genderid
                      and cad.module = 'ADMIN'
                    limit 1
                ) gen on true

                where
                    md.memberid::text ILIKE @starts
                 OR md.firstname ILIKE @contains
                 OR md.lastname ILIKE @contains
                 OR (
                      @digits <> ''
                      AND regexp_replace(coalesce(mp.phone,''), '\D', '', 'g') ILIKE '%' || @digits || '%'
                    )

                order by
                    case when md.memberid::text ILIKE @starts then 0 else 1 end,
                    case when md.lastname ILIKE @contains then 0 else 1 end,
                    md.lastname,
                    md.firstname
                limit @limit;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("starts", $"{q}%");
            cmd.Parameters.AddWithValue("contains", $"%{q}%");
            cmd.Parameters.AddWithValue("digits", qDigits);
            cmd.Parameters.AddWithValue("limit", limit);

            var results = new List<MemberSearchResult>(limit);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new MemberSearchResult
                {
                    memberdetailsid = Convert.ToInt32(reader["memberdetailsid"]),
                    memberid = reader["memberid"]?.ToString(),
                    firstname = reader["firstname"]?.ToString(),
                    lastname = reader["lastname"]?.ToString(),
                    birthdate = reader["birthdate"]?.ToString(),
                    city = reader["city"]?.ToString(),
                    phone = reader["phone"]?.ToString(),
                    gender = reader["gender"]?.ToString()
                });
            }

            return results;
        }

        public async Task<IReadOnlyList<MedicationSearchResult>> SearchMedicationsAsync(
            string q,
            int limit = 25,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Array.Empty<MedicationSearchResult>();

            q = q.Trim();

            await using var conn = GetConnection();
            await conn.OpenAsync(ct);

            const string sql = @"
                select
                    drugname as drugName,
                    ndc as ndc
                from cfgmedicationcodesmaster
                where
                    ndc::text ILIKE @starts
                 or drugname ILIKE @contains
                order by
                    case when ndc::text ILIKE @starts then 0 else 1 end,
                    case when drugname ILIKE @starts then 0 else 1 end,
                    drugname
                limit @limit;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("starts", $"{q}%");
            cmd.Parameters.AddWithValue("contains", $"%{q}%");
            cmd.Parameters.AddWithValue("limit", limit);

            var results = new List<MedicationSearchResult>(limit);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                results.Add(new MedicationSearchResult
                {
                    drugName = reader["drugName"]?.ToString(),
                    ndc = reader["ndc"]?.ToString()
                });
            }

            return results;
        }

        public async Task<IReadOnlyList<StaffSearchResult>> SearchStaffAsync(
            string q,
            int limit = 25,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Array.Empty<StaffSearchResult>();

            q = q.Trim();

            await using var conn = GetConnection();
            await conn.OpenAsync(ct);

            const string sql = @"
                select
                    userdetailid,
                    username
                from securityuser
                where
                    username ILIKE @starts
                 or username ILIKE @contains
                order by
                    case when username ILIKE @starts then 0 else 1 end,
                    username
                limit @limit;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("starts", $"{q}%");
            cmd.Parameters.AddWithValue("contains", $"%{q}%");
            cmd.Parameters.AddWithValue("limit", limit);

            var results = new List<StaffSearchResult>(limit);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                results.Add(new StaffSearchResult
                {
                    userdetailid = Convert.ToInt32(reader["userdetailid"]),
                    username = reader["username"]?.ToString()
                });
            }

            return results;
        }

        public async Task<IReadOnlyList<ProviderSearchResult>> SearchProvidersAsync(
            string q,
            int limit = 25,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Array.Empty<ProviderSearchResult>();

            q = q.Trim();

            await using var conn = GetConnection();
            await conn.OpenAsync(ct);

            const string sql = @"
                select
                    providerid as providerId,   -- <-- change if your PK name differs
                    firstname as firstName,
                    lastname as lastName
                from provider
                where
                    firstname ILIKE @contains
                 or lastname ILIKE @contains
                 or (coalesce(lastname,'') || ', ' || coalesce(firstname,'')) ILIKE @contains
                order by
                    case when lastname ILIKE @starts then 0 else 1 end,
                    lastname,
                    firstname
                limit @limit;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("starts", $"{q}%");
            cmd.Parameters.AddWithValue("contains", $"%{q}%");
            cmd.Parameters.AddWithValue("limit", limit);

            var results = new List<ProviderSearchResult>(limit);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                results.Add(new ProviderSearchResult
                {
                    providerId = Convert.ToInt32(reader["providerId"]),
                    firstName = reader["firstName"]?.ToString(),
                    lastName = reader["lastName"]?.ToString()
                });
            }

            return results;
        }


    }
}
