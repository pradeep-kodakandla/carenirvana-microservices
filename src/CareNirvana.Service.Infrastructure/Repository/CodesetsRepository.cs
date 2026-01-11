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

            // NOTE: If your first/last/email/role columns are in a different table,
            // change the join/column names here.
            const string sql = @"
                select
                    su.userdetailid,
                    su.username,
                    sud.firstname as firstName,
                    sud.lastname as lastName,
                    coalesce( su.username || '@carenirvana.io') as email,
                    'Supervisor' as role,
                    trim(coalesce(sud.firstname,'') || ' ' || coalesce(sud.lastname,'')) as fullName
                from securityuser su
                left join securityuserdetail sud on sud.userdetailid = su.userdetailid
                where
                      su.username ILIKE @starts
                   or su.username ILIKE @contains
                   or coalesce(sud.firstname,'') ILIKE @contains
                   or coalesce(sud.lastname,'') ILIKE @contains
                order by
                    case when su.username ILIKE @starts then 0 else 1 end,
                    su.username
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
                    username = reader["username"]?.ToString(),
                    firstName = reader["firstName"]?.ToString(),
                    lastName = reader["lastName"]?.ToString(),
                    email = reader["email"]?.ToString(),
                    role = reader["role"]?.ToString(),
                    fullName = reader["fullName"]?.ToString()
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

            // Flatten primary address + phone/fax for template fill
            const string sql = @"
        select
            p.providerid as providerId,
            p.firstname as firstName,
            p.middlename as middleName,
            p.lastname as lastName,
            concat(p.prefix, ' ', trim(concat(p.firstname, ' ', coalesce(p.middlename, ''), ' ', p.lastname, ' ', p.suffix))) as fullName,
            p.email,
            p.npi,
            p.organizationname as organizationName,
            p.taxid as taxId,

            adr.addressline1 as addressLine1,
            adr.addressline2 as addressLine2,
            adr.city as city,
            adr.stateid::text as state,
            adr.zipcode as zipCode,

            tel.phone,
            tel.fax

        from public.provider p

        left join lateral (
            select
                a.addressline1, a.addressline2, a.city, a.stateid, a.zipcode
            from public.provideraddress a
            where a.providerid = p.providerid
              and a.activeflag = true
            order by a.isprimary desc, a.provideraddressid desc
            limit 1
        ) adr on true

        left join lateral (
            select
                max(case when pt.contactuse = 'phone' then pt.contactvalue end) as phone,
                max(case when pt.contactuse = 'fax' then pt.contactvalue end) as fax
            from public.providertelecom pt
            where pt.providerid = p.providerid
              and pt.activeflag = true
        ) tel on true

        where p.activeflag = true
          and (
                p.firstname ILIKE @contains
             or p.lastname ILIKE @contains
             or coalesce(p.organizationname,'') ILIKE @contains
             or coalesce(p.npi,'') ILIKE @contains
             or (coalesce(p.lastname,'') || ', ' || coalesce(p.firstname,'')) ILIKE @contains
          )
        order by
            case when p.lastname ILIKE @starts then 0 else 1 end,
            p.lastname,
            p.firstname
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
                    middleName = reader["middleName"]?.ToString(),
                    lastName = reader["lastName"]?.ToString(),
                    fullName = reader["fullName"]?.ToString(),
                    email = reader["email"]?.ToString(),
                    npi = reader["npi"]?.ToString(),
                    organizationName = reader["organizationName"]?.ToString(),
                    taxId = reader["taxId"]?.ToString(),
                    addressLine1 = reader["addressLine1"]?.ToString(),
                    addressLine2 = reader["addressLine2"]?.ToString(),
                    city = reader["city"]?.ToString(),
                    state = reader["state"]?.ToString(),
                    zipCode = reader["zipCode"]?.ToString(),
                    phone = reader["phone"]?.ToString(),
                    fax = reader["fax"]?.ToString(),
                });
            }

            return results;
        }

        public async Task<ProviderDetailResult?> GetProviderByIdAsync(
    int providerId,
    CancellationToken ct = default)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync(ct);

            // Same shape as your GET Provider Query, parameterized
            const string sql = @"
        SELECT
            p.providerid,
            p.firstname,
            p.middlename,
            p.lastname,
            CONCAT(p.prefix, ' ', TRIM(CONCAT(p.firstname, ' ', COALESCE(p.middlename, ''), ' ', p.lastname, ' ', p.suffix))) AS full_name,
            p.email,
            p.npi,
            p.organizationname,

            json_agg(DISTINCT jsonb_build_object(
                'telecomid', pt.providertelecomid,
                'contacttype', pt.contacttypeid,
                'contactvalue', pt.contactvalue,
                'contactuse', pt.contactuse,
                'isprimary', pt.isprimary
            )) FILTER (WHERE pt.providertelecomid IS NOT NULL) AS telecom_info,

            json_agg(DISTINCT jsonb_build_object(
                'addressid', pa.provideraddressid,
                'addresstype', pa.addresstype,
                'addressline1', pa.addressline1,
                'addressline2', pa.addressline2,
                'city', pa.city,
                'state', pa.stateid,
                'zipcode', pa.zipcode,
                'isprimary', pa.isprimary
            )) FILTER (WHERE pa.provideraddressid IS NOT NULL) AS addresses,

            json_agg(DISTINCT jsonb_build_object(
                'licenseid', pl.providerlicenseid,
                'licensetype', pl.licensetype,
                'licensenumber', pl.licensenumber,
                'stateid', pl.stateid,
                'issuedate', pl.issuedate,
                'expirationdate', pl.expirationdate,
                'status', pl.status
            )) FILTER (WHERE pl.providerlicenseid IS NOT NULL) AS licenses,

            json_agg(DISTINCT jsonb_build_object(
                'certificationid', pbc.providerboardcertificationid,
                'boardname', pbc.boardname,
                'certificationnumber', pbc.certificationnumber,
                'expirationdate', pbc.expirationdate,
                'status', pbc.status
            )) FILTER (WHERE pbc.providerboardcertificationid IS NOT NULL) AS board_certifications,

            json_agg(DISTINCT jsonb_build_object(
                'educationid', pe.providereducationid,
                'institutionname', pe.institutionname,
                'degreetype', pe.degreetype,
                'fieldofstudy', pe.fieldofstudy,
                'completiondate', pe.completiondate
            )) FILTER (WHERE pe.providereducationid IS NOT NULL) AS education,

            json_agg(DISTINCT jsonb_build_object(
                'identifierid', ppi.provideridentifierid,
                'identifiertype', ppi.identifiertypeid,
                'identifiervalue', ppi.identifiervalue,
                'expirationdate', ppi.expirationdate
            )) FILTER (WHERE ppi.provideridentifierid IS NOT NULL) AS identifiers,

            json_agg(DISTINCT jsonb_build_object(
                'languageid', plang.providerlanguageid,
                'language', plang.languageid,
                'proficiency', plang.proficiencylevel
            )) FILTER (WHERE plang.providerlanguageid IS NOT NULL) AS languages,

            json_agg(DISTINCT jsonb_build_object(
                'networkid', pn.providernetworkid,
                'networkname', pn.networkname,
                'payerid', pn.payerid,
                'payername', pn.payername,
                'status', pn.status
            )) FILTER (WHERE pn.providernetworkid IS NOT NULL) AS networks,

            json_agg(DISTINCT jsonb_build_object(
                'credentialingid', pc.providercredentialingid,
                'credentialingstartdate', pc.credentialingstartdate,
                'completiondate', pc.credentialingcompletiondate,
                'recredentialingduedate', pc.recredentialingduedate,
                'primarysourceverified', pc.primarysourceverified
            )) FILTER (WHERE pc.providercredentialingid IS NOT NULL) AS credentialing,

            json_agg(DISTINCT jsonb_build_object(
                'insuranceid', pli.providerliabilityinsuranceid,
                'insurancecarrier', pli.insurancecarrier,
                'policynumber', pli.policynumber,
                'coveragetype', pli.coveragetype,
                'coverageamount', pli.coverageamount,
                'expirationdate', pli.expirationdate
            )) FILTER (WHERE pli.providerliabilityinsuranceid IS NOT NULL) AS liability_insurance,

            json_agg(DISTINCT jsonb_build_object(
                'accreditationid', pacc.provideraccreditationid,
                'accreditationbody', pacc.accreditationbody,
                'accreditationtype', pacc.accreditationtype,
                'expirationdate', pacc.expirationdate,
                'status', pacc.status
            )) FILTER (WHERE pacc.provideraccreditationid IS NOT NULL) AS accreditations,

            json_agg(DISTINCT jsonb_build_object(
                'roleid', pr.providerroleid,
                'rolename', pr.rolename,
                'effectivedate', pr.effectivedate,
                'expirationdate', pr.expirationdate
            )) FILTER (WHERE pr.providerroleid IS NOT NULL) AS roles,

            p.active,
            p.acceptingnewpatients,
            p.createdon,
            p.updatedon

        FROM public.provider p
        LEFT JOIN public.providertelecom pt ON p.providerid = pt.providerid AND pt.activeflag = true
        LEFT JOIN public.provideraddress pa ON p.providerid = pa.providerid AND pa.activeflag = true
        LEFT JOIN public.providerlicense pl ON p.providerid = pl.providerid AND pl.activeflag = true
        LEFT JOIN public.providerboardcertification pbc ON p.providerid = pbc.providerid AND pbc.activeflag = true
        LEFT JOIN public.providereducation pe ON p.providerid = pe.providerid AND pe.activeflag = true
        LEFT JOIN public.provideridentifier ppi ON p.providerid = ppi.providerid AND ppi.activeflag = true
        LEFT JOIN public.providerlanguage plang ON p.providerid = plang.providerid AND plang.activeflag = true
        LEFT JOIN public.providernetwork pn ON p.providerid = pn.providerid AND pn.activeflag = true
        LEFT JOIN public.providercredentialing pc ON p.providerid = pc.providerid AND pc.activeflag = true
        LEFT JOIN public.providerliabilityinsurance pli ON p.providerid = pli.providerid AND pli.activeflag = true
        LEFT JOIN public.provideraccreditation pacc ON p.providerid = pacc.providerid AND pacc.activeflag = true
        LEFT JOIN public.providerrole pr ON p.providerid = pr.practitionerproviderid AND pr.activeflag = true
        WHERE p.providerid = @providerId
          AND p.activeflag = true
        GROUP BY p.providerid, p.firstname, p.middlename, p.lastname, p.prefix, p.suffix,
                 p.email, p.npi, p.organizationname, p.active, p.acceptingnewpatients,
                 p.createdon, p.updatedon, p.taxid, p.activeflag;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("providerId", providerId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return null;

            string? JsonOrNull(string col) => reader[col] == DBNull.Value ? null : reader[col]?.ToString();

            return new ProviderDetailResult
            {
                providerId = Convert.ToInt32(reader["providerid"]),
                firstName = reader["firstname"]?.ToString(),
                middleName = reader["middlename"]?.ToString(),
                lastName = reader["lastname"]?.ToString(),
                fullName = reader["full_name"]?.ToString(),
                email = reader["email"]?.ToString(),
                npi = reader["npi"]?.ToString(),
                organizationName = reader["organizationname"]?.ToString(),

                telecomInfoJson = JsonOrNull("telecom_info"),
                addressesJson = JsonOrNull("addresses"),
                licensesJson = JsonOrNull("licenses"),
                boardCertificationsJson = JsonOrNull("board_certifications"),
                educationJson = JsonOrNull("education"),
                identifiersJson = JsonOrNull("identifiers"),
                languagesJson = JsonOrNull("languages"),
                networksJson = JsonOrNull("networks"),
                credentialingJson = JsonOrNull("credentialing"),
                liabilityInsuranceJson = JsonOrNull("liability_insurance"),
                accreditationsJson = JsonOrNull("accreditations"),
                rolesJson = JsonOrNull("roles"),

                active = reader["active"] == DBNull.Value ? null : (bool?)Convert.ToBoolean(reader["active"]),
                acceptingNewPatients = reader["acceptingnewpatients"] == DBNull.Value ? null : (bool?)Convert.ToBoolean(reader["acceptingnewpatients"]),
                createdOn = reader["createdon"] as DateTime?,
                updatedOn = reader["updatedon"] as DateTime?
            };
        }

        public async Task<IReadOnlyList<ClaimSearchResult>> SearchClaimsAsync(
    string q,
    int limit = 25,
    CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Array.Empty<ClaimSearchResult>();

            q = q.Trim();

            await using var conn = GetConnection();
            await conn.OpenAsync(ct);

            const string sql = @"
        select
            mch.memberclaimheaderid as memberClaimHeaderId,
            mch.memberdetailsid as memberDetailsId,
            mch.claimnumber as claimNumber,
            mch.providerid as providerId,
            coalesce(p.organizationname, trim(coalesce(p.firstname,'') || ' ' || coalesce(p.lastname,''))) as providerName,
            mch.dos_from as dosFrom,
            mch.dos_to as dosTo,
            mch.visittypeid as visitTypeId,
            mch.reasonforvisit as reasonForVisit,

            coalesce(sum(mcl.originallineamount),0) as billed,
            coalesce(sum(mcl.allowedamount),0) as allowedAmount,
            coalesce(sum(mcl.copayamount),0) as copayAmount,
            coalesce(sum(mcp.paymentamount),0) as paid

        from public.memberclaimheader mch
        left join public.memberclaimline mcl
            on mch.memberclaimheaderid = mcl.memberclaimheaderid
           and mcl.activeflag = true
        left join public.memberclaimpayment mcp
            on mch.memberclaimheaderid = mcp.memberclaimheaderid
           and mcp.activeflag = true
        left join public.provider p
            on p.providerid = mch.providerid
           and p.activeflag = true

        where mch.activeflag = true
          and (
                mch.claimnumber::text ILIKE @starts
             or mch.claimnumber::text ILIKE @contains
             or coalesce(p.organizationname,'') ILIKE @contains
             or coalesce(p.firstname,'') ILIKE @contains
             or coalesce(p.lastname,'') ILIKE @contains
          )

        group by
            mch.memberclaimheaderid, mch.memberdetailsid, mch.claimnumber, mch.providerid,
            p.organizationname, p.firstname, p.lastname,
            mch.dos_from, mch.dos_to, mch.visittypeid, mch.reasonforvisit

        order by
            case when mch.claimnumber::text ILIKE @starts then 0 else 1 end,
            mch.claimnumber
        limit @limit;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("starts", $"{q}%");
            cmd.Parameters.AddWithValue("contains", $"%{q}%");
            cmd.Parameters.AddWithValue("limit", limit);

            var results = new List<ClaimSearchResult>(limit);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                results.Add(new ClaimSearchResult
                {
                    memberClaimHeaderId = Convert.ToInt64(reader["memberClaimHeaderId"]),
                    memberDetailsId = reader["memberDetailsId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["memberDetailsId"]),
                    claimNumber = reader["claimNumber"]?.ToString(),
                    providerId = reader["providerId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["providerId"]),
                    providerName = reader["providerName"]?.ToString(),
                    dosFrom = reader["dosFrom"] as DateTime?,
                    dosTo = reader["dosTo"] as DateTime?,
                    visitTypeId = reader["visitTypeId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["visitTypeId"]),
                    reasonForVisit = reader["reasonForVisit"]?.ToString(),
                    billed = reader["billed"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["billed"]),
                    allowedAmount = reader["allowedAmount"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["allowedAmount"]),
                    copayAmount = reader["copayAmount"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["copayAmount"]),
                    paid = reader["paid"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["paid"]),
                });
            }

            return results;
        }


        public async Task<ClaimDetailResult?> GetClaimByIdAsync(
    long memberClaimHeaderId,
    CancellationToken ct = default)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync(ct);

            const string sql = @"
        SELECT 
            mch.memberclaimheaderid,
            mch.memberdetailsid,
            mch.claimnumber,
            mch.claimtypeid,
            mch.billtype,
            mch.providerid,
            mch.enrollmenthierarchyid,
            mch.companycode,
            mch.patcontrrolnumber,
            mch.authnumber,
            mch.visittypeid,
            mch.reasonforvisit,
            mch.dos_from,
            mch.dos_to,
            mch.los,
            mch.placeofserviceid,
            mch.medicalrecordnumber,
            mch.programtype,
            mch.claimstatusid,
            mch.holdcodeid,
            mch.notes,
            mch.receiveddate,
            mch.paiddate,
            mch.checkdate,
            mch.checknumber,
            mch.issensitive,

            -- totals for template fill
            coalesce(sum(distinct mcl.originallineamount),0) as billed,
            coalesce(sum(distinct mcl.allowedamount),0) as allowedamount,
            coalesce(sum(distinct mcl.copayamount),0) as copayamount,
            coalesce(sum(distinct mcp.paymentamount),0) as paid,

            json_agg(DISTINCT jsonb_build_object(
                'claimlineid', mcl.memberclaimlineid,
                'claimline', mcl.claimline,
                'dos_from', mcl.dos_from,
                'dos_to', mcl.dos_to,
                'placeofserviceid', mcl.placeofserviceid,
                'revenuecodeid', mcl.revenuecodeid,
                'servicecodeid', mcl.servicecodeid,
                'cptmodifierid', mcl.cptmodifierid,
                'units', mcl.units,
                'qualifiercode', mcl.qualifiercode,
                'originaldamountagreed', mcl.originaldamountagreed,
                'originallineamount', mcl.originallineamount,
                'allowedamount', mcl.allowedamount,
                'deductibleamount', mcl.deductibleamount,
                'coinsuranceamount', mcl.coinsuranceamount,
                'copayamount', mcl.copayamount,
                'netamount', mcl.netamount,
                'noncoveredflag', mcl.noncoveredflag,
                'postdate', mcl.postdate,
                'paiddate', mcl.paiddate,
                'procedurelinestatusid', mcl.procedurelinestatusid,
                'notes', mcl.notes
            )) FILTER (WHERE mcl.memberclaimlineid IS NOT NULL) AS claim_lines,

            json_agg(DISTINCT jsonb_build_object(
                'diagnosisid', mcd.memberclaimdiagnosisid,
                'icdcodeid', mcd.icdcodeid,
                'diagnosissequence', mcd.diagnosissequence,
                'isprimary', mcd.isprimary
            )) FILTER (WHERE mcd.memberclaimdiagnosisid IS NOT NULL) AS diagnoses,

            json_agg(DISTINCT jsonb_build_object(
                'paymentid', mcp.memberclaimpaymentid,
                'paymentmethod', mcp.paymentmethod,
                'checknumber', mcp.checknumber,
                'paymentamount', mcp.paymentamount,
                'paymentdate', mcp.paymentdate,
                'paymentstatus', mcp.paymentstatus,
                'remittanceadvicenumber', mcp.remittanceadvicenumber
            )) FILTER (WHERE mcp.memberclaimpaymentid IS NOT NULL) AS payments,

            json_agg(DISTINCT jsonb_build_object(
                'documentid', mcd2.memberclaimdocumentid,
                'documenttype', mcd2.documenttype,
                'documentname', mcd2.documentname,
                'documentpath', mcd2.documentpath,
                'documentsize', mcd2.documentsize,
                'documentextension', mcd2.documentextension,
                'documentuploaddate', mcd2.documentuploaddate
            )) FILTER (WHERE mcd2.memberclaimdocumentid IS NOT NULL) AS documents,

            mch.createdon,
            mch.createdby,
            mch.updatedon,
            mch.updatedby

        FROM public.memberclaimheader mch
        LEFT JOIN public.memberclaimline mcl ON mch.memberclaimheaderid = mcl.memberclaimheaderid AND mcl.activeflag = true
        LEFT JOIN public.memberclaimdiagnosis mcd ON mch.memberclaimheaderid = mcd.memberclaimheaderid AND mcd.activeflag = true
        LEFT JOIN public.memberclaimpayment mcp ON mch.memberclaimheaderid = mcp.memberclaimheaderid AND mcp.activeflag = true
        LEFT JOIN public.memberclaimdocument mcd2 ON mch.memberclaimheaderid = mcd2.memberclaimheaderid AND mcd2.activeflag = true
        WHERE mch.memberclaimheaderid = @id
          AND mch.activeflag = true
        GROUP BY mch.memberclaimheaderid, mch.memberdetailsid, mch.claimnumber, mch.claimtypeid,
                 mch.billtype, mch.providerid, mch.enrollmenthierarchyid, mch.companycode,
                 mch.patcontrrolnumber, mch.authnumber, mch.visittypeid, mch.reasonforvisit,
                 mch.dos_from, mch.dos_to, mch.los, mch.placeofserviceid, mch.medicalrecordnumber,
                 mch.programtype, mch.claimstatusid, mch.holdcodeid, mch.notes, mch.receiveddate,
                 mch.paiddate, mch.checkdate, mch.checknumber, mch.issensitive,
                 mch.createdon, mch.createdby, mch.updatedon, mch.updatedby;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", memberClaimHeaderId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return null;

            string? JsonOrNull(string col) => reader[col] == DBNull.Value ? null : reader[col]?.ToString();

            return new ClaimDetailResult
            {
                memberClaimHeaderId = Convert.ToInt64(reader["memberclaimheaderid"]),
                memberDetailsId = reader["memberdetailsid"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["memberdetailsid"]),
                claimNumber = reader["claimnumber"]?.ToString(),
                claimTypeId = reader["claimtypeid"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["claimtypeid"]),
                billType = reader["billtype"]?.ToString(),
                providerId = reader["providerid"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["providerid"]),
                enrollmentHierarchyId = reader["enrollmenthierarchyid"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["enrollmenthierarchyid"]),
                companyCode = reader["companycode"]?.ToString(),
                patContrrolNumber = reader["patcontrrolnumber"]?.ToString(),
                authNumber = reader["authnumber"]?.ToString(),
                visitTypeId = reader["visittypeid"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["visittypeid"]),
                reasonForVisit = reader["reasonforvisit"]?.ToString(),
                dosFrom = reader["dos_from"] as DateTime?,
                dosTo = reader["dos_to"] as DateTime?,
                los = reader["los"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["los"]),
                placeOfServiceId = reader["placeofserviceid"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["placeofserviceid"]),
                medicalRecordNumber = reader["medicalrecordnumber"]?.ToString(),
                programType = reader["programtype"]?.ToString(),
                claimStatusId = reader["claimstatusid"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["claimstatusid"]),
                holdCodeId = reader["holdcodeid"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["holdcodeid"]),
                notes = reader["notes"]?.ToString(),
                receivedDate = reader["receiveddate"] as DateTime?,
                paidDate = reader["paiddate"] as DateTime?,
                checkDate = reader["checkdate"] as DateTime?,
                checkNumber = reader["checknumber"]?.ToString(),
                isSensitive = reader["issensitive"] == DBNull.Value ? null : (bool?)Convert.ToBoolean(reader["issensitive"]),

                billed = reader["billed"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["billed"]),
                allowedAmount = reader["allowedamount"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["allowedamount"]),
                copayAmount = reader["copayamount"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["copayamount"]),
                paid = reader["paid"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["paid"]),

                claimLinesJson = JsonOrNull("claim_lines"),
                diagnosesJson = JsonOrNull("diagnoses"),
                paymentsJson = JsonOrNull("payments"),
                documentsJson = JsonOrNull("documents"),

                createdOn = reader["createdon"] as DateTime?,
                createdBy = reader["createdby"]?.ToString(),
                updatedOn = reader["updatedon"] as DateTime?,
                updatedBy = reader["updatedby"]?.ToString()
            };
        }


    }
}
