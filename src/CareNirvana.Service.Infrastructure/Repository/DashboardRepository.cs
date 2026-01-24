using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;
using System.Collections.Generic;
using System.Data;
using System.Net.NetworkInformation;
using static System.Net.Mime.MediaTypeNames;

namespace CareNirvana.Service.Infrastructure.Repository
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly string _connectionString;

        public DashboardRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public async Task<List<MemberCareStaff>> GetMyCareStaff(int userId)
        {
            var careStaffList = new List<MemberCareStaff>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT * 
                         FROM membercarestaff 
                         WHERE userid = @userId AND activeflag = true";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var staff = new MemberCareStaff
                            {
                                MemberCareStaffId = reader.GetInt32(reader.GetOrdinal("membercarestaffid")),
                                UserId = reader.IsDBNull(reader.GetOrdinal("userid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("userid")),
                                MemberDetailsId = reader.IsDBNull(reader.GetOrdinal("memberdetailsid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberdetailsid")),
                                ActiveFlag = reader.GetBoolean(reader.GetOrdinal("activeflag")),

                                StartDate = reader.GetDateTime(reader.GetOrdinal("startdate")),
                                EndDate = reader.GetDateTime(reader.GetOrdinal("enddate")),

                                CreatedOn = reader.GetDateTime(reader.GetOrdinal("createdon")),
                                CreatedBy = reader.IsDBNull(reader.GetOrdinal("createdby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("createdby")),

                                UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updatedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("updatedon")),
                                UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updatedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("updatedby")),

                                DeletedOn = reader.IsDBNull(reader.GetOrdinal("deletedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("deletedon")),
                                DeletedBy = reader.IsDBNull(reader.GetOrdinal("deletedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("deletedby"))
                            };

                            careStaffList.Add(staff);
                        }
                    }
                }
            }

            return careStaffList;
        }

        public async Task<DashboardCounts> DashBoardCount(int userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // One round-trip: compute all counts using scalar subqueries
            const string sql = @"
           SELECT
                  (SELECT COUNT(DISTINCT m.memberdetailsid)
                   FROM public.membercarestaff m
                   WHERE m.userid = @userId
                     AND COALESCE(m.activeflag, true) = true
                  ) AS mymembercount,

                  (SELECT COUNT(*)
                   FROM public.authdetail a
                   WHERE a.authassignedto = @userId
                  ) AS authcount,

                  (
                    WITH pending AS (

                      -- CM
                      SELECT DISTINCT maw.memberactivityworkgroupid AS item_id
                      FROM cfguserworkgroup cug
                      JOIN cfgworkgroupworkbasket cww
                        ON cww.workgroupworkbasketid = cug.workgroupworkbasketid
                      JOIN memberactivityworkgroup maw
                        ON maw.workgroupworkbasketid = cug.workgroupworkbasketid
                      JOIN memberactivity ma
                        ON ma.memberactivityid = maw.memberactivityid
                      WHERE COALESCE(cug.activeflag, TRUE) = TRUE
                        AND COALESCE(maw.activeflag, TRUE) = TRUE
                        AND (@userId IS NULL OR cug.userid = @userId)
                        AND ma.referto IS NULL
                        AND ma.isworkbasket = TRUE
                        AND ma.deletedon IS NULL
                        AND COALESCE(ma.activeflag, TRUE) = TRUE

                      UNION ALL

                      -- UM (AUTH)
                      SELECT DISTINCT awg.authworkgroupid AS item_id
                      FROM cfguserworkgroup cug
                      JOIN cfgworkgroupworkbasket cww
                        ON cww.workgroupworkbasketid = cug.workgroupworkbasketid
                      JOIN authworkgroup awg
                        ON awg.workgroupworkbasketid = cug.workgroupworkbasketid
                       AND awg.requesttype = 'AUTH'
                       AND COALESCE(awg.activeflag, TRUE) = TRUE
                      JOIN authdetail a
                        ON a.authdetailid = awg.authdetailid
                      WHERE COALESCE(cug.activeflag, TRUE) = TRUE
                        AND (@userId IS NULL OR cug.userid = @userId)
                        AND a.deletedon IS NULL
                        AND a.authassignedto IS NULL
                        AND NOT EXISTS (
                          SELECT 1
                          FROM authworkgroupaction awa
                          WHERE awa.authworkgroupid = awg.authworkgroupid
                            AND COALESCE(awa.activeflag, TRUE) = TRUE
                            AND upper(awa.actiontype) IN ('ACCEPT','ACCEPTED')
                          LIMIT 1
                        )

                      UNION ALL

                      -- AG (CASE)
                      SELECT DISTINCT cw.caseworkgroupid AS item_id
                      FROM cfguserworkgroup cug
                      JOIN cfgworkgroupworkbasket cww
                        ON cww.workgroupworkbasketid = cug.workgroupworkbasketid
                      JOIN caseworkgroup cw
                        ON cw.workgroupworkbasketid = cug.workgroupworkbasketid
                       AND cw.requesttype = 'CASE'
                       AND COALESCE(cw.activeflag, TRUE) = TRUE
                      WHERE COALESCE(cug.activeflag, TRUE) = TRUE
                        AND (@userId IS NULL OR cug.userid = @userId)
                        AND NOT EXISTS (
                          SELECT 1
                          FROM caseworkgroupaction cwa
                          WHERE cwa.caseworkgroupid = cw.caseworkgroupid
                            AND COALESCE(cwa.activeflag, TRUE) = TRUE
                            AND upper(cwa.actiontype) IN ('ACCEPT','ACCEPTED')
                          LIMIT 1
                        )
                    )
                    SELECT COUNT(*) FROM pending
                  ) AS requestcount,

                  (SELECT COUNT(*)
                   FROM public.authactivity aa
                   WHERE aa.referto = @userId
                     AND aa.service_line_count = 0
                  ) AS activitycount,

                  (SELECT COUNT(*)
                   FROM public.authactivity aa
                   WHERE aa.referto = @userId
                     AND aa.service_line_count <> 0
                     AND md_review_status <> 'Approved'
                  ) AS wqcount,

                  (SELECT COUNT(*)
                   FROM public.CASEHEADER ch
                   WHERE ch.createdby = @userId
                  ) AS casecount,

                  (SELECT COUNT(*) FROM public.faxfiles) AS faxcount;
                ";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (await reader.ReadAsync())
            {
                return new DashboardCounts
                {
                    MyMemberCount = reader.GetInt32(reader.GetOrdinal("mymembercount")),
                    AuthCount = reader.GetInt32(reader.GetOrdinal("authcount")),
                    ActivityCount = reader.GetInt32(reader.GetOrdinal("activitycount")),
                    RequestCount = reader.GetInt32(reader.GetOrdinal("requestcount")),
                    ComplaintCount = reader.GetInt32(reader.GetOrdinal("casecount")),
                    FaxCount = reader.GetInt32(reader.GetOrdinal("faxcount")),
                    WQCount = reader.GetInt32(reader.GetOrdinal("wqcount"))
                };
            }

            // No rows (shouldn't happen with this query), return zeros
            return new DashboardCounts();
        }
        public async Task<List<MemberSummary>> GetMemberSummaries(int userId)
        {
            var results = new List<MemberSummary>();

            const string sql = @"
                                    select distinct 
                                    md.firstname,
                                    md.lastname,
                                    md.memberid,
                                    md.memberdetailsid,
                                    to_char(md.birthdate::date, 'MM-DD-YYYY') as birthdate,
                                    mr.riskscore,
                                    mr.risklevelid,
                                    rl.risklevel_code,
                                    null as lastcontact,
                                    null as nextcontact,
                                    ma.city,
                                    mp.phonenumber as memberphonenumberid,
                                    hie.level_map,
	                                hie.enddate as enrollmentenddate,
                                    mc.startdate,
                                    mc.enddate,
	                                gen.gender,
	                                prog.programs,
                                    coalesce(ac.authcount, 0) as authcount,
                                    COALESCE(al.alertcount, 0)   AS alertcount
                                from membercarestaff mc
                                join memberdetails md
                                  on md.memberdetailsid = mc.memberdetailsid

                                left join memberrisk mr
                                  on mr.memberriskid = (
                                        select mr2.memberriskid
                                        from memberrisk mr2
                                        where mr2.memberdetailsid = md.memberdetailsid
                                        order by mr2.riskenddate desc
                                        limit 1
                                     )
                                left join memberaddress ma 
                                  on ma.memberdetailsid = md.memberdetailsid and ma.isprimary = true
                                left join memberphonenumber mp 
                                  on mp.memberdetailsid = md.memberdetailsid and mp.ispreferred = true
                                left join vw_member_enrollment_hierarchy_json hie
                                  on hie.memberdetailsid = md.memberdetailsid and hie.enddate > current_date
                                left join lateral (
                                    select elem->>'code' as risklevel_code
                                    from cfgadmindata cad,
                                         jsonb_array_elements(cad.jsoncontent::jsonb->'risklevel') elem
                                    where (elem->>'id')::int = mr.risklevelid
                                      and cad.module = 'CM'
                                    limit 1
                                ) rl on true
                                left join lateral (
                                    select elem->>'gender' as gender
                                    from cfgadmindata cad,
                                         jsonb_array_elements(cad.jsoncontent::jsonb->'gender') elem
                                    where (elem->>'id')::int = md.genderid
                                      and cad.module = 'ADMIN'
                                    limit 1
                                ) gen on true
                                left join lateral (
                                    select
                                        string_agg(distinct (elem->>'programName'), ', ' order by elem->>'programName') as programs,
                                        array_agg(distinct mp2.programid)                                            as program_ids
                                    from memberprogram mp2
                                    join cfgadmindata cad
                                      on cad.module = 'CM'
                                    cross join lateral jsonb_array_elements(cad.jsoncontent::jsonb->'program') elem
                                    where mp2.memberdetailsid = mc.memberdetailsid
                                      and coalesce(mp2.activeflag, true) = true
                                      and (elem->>'id')::int = mp2.programid
                                ) prog on true
                                left join (
                                    select ad.memberdetailsid, count(*) as authcount
                                    from authdetail ad
                                    group by ad.memberdetailsid
                                ) ac on ac.memberdetailsid = md.memberdetailsid
                                LEFT JOIN (
                                    SELECT ma2.memberdetailsid, COUNT(*) AS alertcount
                                    FROM memberalert ma2
                                    WHERE COALESCE(ma2.activeflag, TRUE) = TRUE
                                    GROUP BY ma2.memberdetailsid
                                ) al ON al.memberdetailsid = md.memberdetailsid
                                where mc.userid = @userId
                                  and mc.activeflag = true;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            while (await reader.ReadAsync())
            {
                var o = new MemberSummary
                {
                    FirstName = reader.IsDBNull(reader.GetOrdinal("firstname")) ? null : reader.GetString(reader.GetOrdinal("firstname")),
                    LastName = reader.IsDBNull(reader.GetOrdinal("lastname")) ? null : reader.GetString(reader.GetOrdinal("lastname")),
                    MemberId = reader.IsDBNull(reader.GetOrdinal("memberid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberid")),
                    MemberDetailsId = reader.IsDBNull(reader.GetOrdinal("memberdetailsid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberdetailsid")),
                    DOB = reader.IsDBNull(reader.GetOrdinal("birthdate")) ? null : reader.GetString(reader.GetOrdinal("birthdate")),

                    RiskScore = reader.IsDBNull(reader.GetOrdinal("riskscore")) ? (decimal?)null : reader.GetFieldValue<decimal>(reader.GetOrdinal("riskscore")),
                    RiskLevelId = reader.IsDBNull(reader.GetOrdinal("risklevelid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("risklevelid")),
                    RiskLevelCode = reader.IsDBNull(reader.GetOrdinal("risklevel_code")) ? null : reader.GetString(reader.GetOrdinal("risklevel_code")),

                    LastContact = reader.IsDBNull(reader.GetOrdinal("lastcontact")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("lastcontact")),
                    NextContact = reader.IsDBNull(reader.GetOrdinal("nextcontact")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("nextcontact")),

                    City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString(reader.GetOrdinal("city")),
                    MemberPhoneNumberId = reader.IsDBNull(reader.GetOrdinal("memberphonenumberid")) ? (string?)null : reader.GetString(reader.GetOrdinal("memberphonenumberid")),
                    LevelMap = reader.IsDBNull(reader.GetOrdinal("level_map")) ? null : reader.GetString(reader.GetOrdinal("level_map")),
                    EnrollmentEndDate = reader.IsDBNull(reader.GetOrdinal("enrollmentenddate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("enrollmentenddate")),
                    StartDate = reader.IsDBNull(reader.GetOrdinal("startdate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("startdate")),
                    EndDate = reader.IsDBNull(reader.GetOrdinal("enddate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("enddate")),

                    Gender = reader.IsDBNull(reader.GetOrdinal("gender")) ? null : reader.GetString(reader.GetOrdinal("gender")),
                    Programs = reader.IsDBNull(reader.GetOrdinal("programs")) ? null : reader.GetString(reader.GetOrdinal("programs")),
                    AuthCount = reader.GetInt32(reader.GetOrdinal("authcount")),
                    AlertCount = reader.GetInt32(reader.GetOrdinal("alertcount"))
                };

                results.Add(o);
            }

            return results;
        }

        public async Task<List<MemberSummary>> GetMemberSummary(int memberdetailsid)
        {
            var results = new List<MemberSummary>();

            const string sql = @"
                                    select distinct 
                                    md.firstname,
                                    md.lastname,
                                    md.memberid,
                                    md.memberdetailsid,
                                    to_char(md.birthdate::date, 'MM-DD-YYYY') as birthdate,
                                    mr.riskscore,
                                    mr.risklevelid,
                                    rl.risklevel_code,
                                    null as lastcontact,
                                    null as nextcontact,
                                    ma.city,
                                    mp.phonenumber as memberphonenumberid,
                                    hie.level_map,
	                                hie.enddate as enrollmentenddate,
                                    mc.startdate,
                                    mc.enddate,
	                                gen.gender,
	                                prog.programs,
                                    coalesce(ac.authcount, 0) as authcount,
                                    COALESCE(al.alertcount, 0)   AS alertcount
                                from membercarestaff mc
                                join memberdetails md
                                  on md.memberdetailsid = mc.memberdetailsid

                                left join memberrisk mr
                                  on mr.memberriskid = (
                                        select mr2.memberriskid
                                        from memberrisk mr2
                                        where mr2.memberdetailsid = md.memberdetailsid
                                        order by mr2.riskenddate desc
                                        limit 1
                                     )
                                left join memberaddress ma 
                                  on ma.memberdetailsid = md.memberdetailsid and ma.isprimary = true
                                left join memberphonenumber mp 
                                  on mp.memberdetailsid = md.memberdetailsid and mp.ispreferred = true
                                left join vw_member_enrollment_hierarchy_json hie
                                  on hie.memberdetailsid = md.memberdetailsid
                                left join lateral (
                                    select elem->>'code' as risklevel_code
                                    from cfgadmindata cad,
                                         jsonb_array_elements(cad.jsoncontent::jsonb->'risklevel') elem
                                    where (elem->>'id')::int = mr.risklevelid
                                      and cad.module = 'CM'
                                    limit 1
                                ) rl on true
                                left join lateral (
                                    select elem->>'gender' as gender
                                    from cfgadmindata cad,
                                         jsonb_array_elements(cad.jsoncontent::jsonb->'gender') elem
                                    where (elem->>'id')::int = md.genderid
                                      and cad.module = 'ADMIN'
                                    limit 1
                                ) gen on true
                                left join lateral (
                                    select
                                        string_agg(distinct (elem->>'programName'), ', ' order by elem->>'programName') as programs,
                                        array_agg(distinct mp2.programid)                                            as program_ids
                                    from memberprogram mp2
                                    join cfgadmindata cad
                                      on cad.module = 'CM'
                                    cross join lateral jsonb_array_elements(cad.jsoncontent::jsonb->'program') elem
                                    where mp2.memberdetailsid = mc.memberdetailsid
                                      and coalesce(mp2.activeflag, true) = true
                                      and (elem->>'id')::int = mp2.programid
                                ) prog on true
                                left join (
                                    select ad.memberdetailsid, count(*) as authcount
                                    from authdetail ad
                                    group by ad.memberdetailsid
                                ) ac on ac.memberdetailsid = md.memberdetailsid
                                LEFT JOIN (
                                    SELECT ma2.memberdetailsid, COUNT(*) AS alertcount
                                    FROM memberalert ma2
                                    WHERE COALESCE(ma2.activeflag, TRUE) = TRUE
                                    GROUP BY ma2.memberdetailsid
                                ) al ON al.memberdetailsid = md.memberdetailsid
                                where mc.memberdetailsid = @memberdetailsid
                                  and mc.activeflag = true;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@memberdetailsid", memberdetailsid);

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            while (await reader.ReadAsync())
            {
                var o = new MemberSummary
                {
                    FirstName = reader.IsDBNull(reader.GetOrdinal("firstname")) ? null : reader.GetString(reader.GetOrdinal("firstname")),
                    LastName = reader.IsDBNull(reader.GetOrdinal("lastname")) ? null : reader.GetString(reader.GetOrdinal("lastname")),
                    MemberId = reader.IsDBNull(reader.GetOrdinal("memberid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberid")),
                    MemberDetailsId = reader.IsDBNull(reader.GetOrdinal("memberdetailsid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberdetailsid")),
                    DOB = reader.IsDBNull(reader.GetOrdinal("birthdate")) ? null : reader.GetString(reader.GetOrdinal("birthdate")),

                    RiskScore = reader.IsDBNull(reader.GetOrdinal("riskscore")) ? (decimal?)null : reader.GetFieldValue<decimal>(reader.GetOrdinal("riskscore")),
                    RiskLevelId = reader.IsDBNull(reader.GetOrdinal("risklevelid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("risklevelid")),
                    RiskLevelCode = reader.IsDBNull(reader.GetOrdinal("risklevel_code")) ? null : reader.GetString(reader.GetOrdinal("risklevel_code")),

                    LastContact = reader.IsDBNull(reader.GetOrdinal("lastcontact")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("lastcontact")),
                    NextContact = reader.IsDBNull(reader.GetOrdinal("nextcontact")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("nextcontact")),

                    City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString(reader.GetOrdinal("city")),
                    MemberPhoneNumberId = reader.IsDBNull(reader.GetOrdinal("memberphonenumberid")) ? (String?)null : reader.GetString(reader.GetOrdinal("memberphonenumberid")),
                    LevelMap = reader.IsDBNull(reader.GetOrdinal("level_map")) ? null : reader.GetString(reader.GetOrdinal("level_map")),
                    EnrollmentEndDate = reader.IsDBNull(reader.GetOrdinal("enrollmentenddate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("enrollmentenddate")),
                    StartDate = reader.IsDBNull(reader.GetOrdinal("startdate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("startdate")),
                    EndDate = reader.IsDBNull(reader.GetOrdinal("enddate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("enddate")),

                    Gender = reader.IsDBNull(reader.GetOrdinal("gender")) ? null : reader.GetString(reader.GetOrdinal("gender")),
                    Programs = reader.IsDBNull(reader.GetOrdinal("programs")) ? null : reader.GetString(reader.GetOrdinal("programs")),
                    AuthCount = reader.GetInt32(reader.GetOrdinal("authcount")),
                    AlertCount = reader.GetInt32(reader.GetOrdinal("alertcount"))
                };

                results.Add(o);
            }

            return results;
        }

        public async Task<List<AuthDetailListItem>> GetAuthDetailListAsync(int userId)
        {
            const string sql = @"
                    SELECT
                        ad.authdetailid,
                        ad.authnumber,
                        ad.authstatus,
                        rl.authstatusvalue,
                        at.authtemplatename AS templatename,
                        at.authtemplateid,
                        ac.authclassvalue,
                        ad.memberdetailsid,
                        md.memberid,
                        ad.nextreviewdate,
                        ad.authduedate,
                        ad.createdon,
                        ad.createdby,
                        su.username AS createduser,
                        ad.updatedon,
                        ad.updatedby,

                        -- values stored at root level in JSON
                        ah.treatmenttype_json AS treatmenttype,
                        tt.treatmenttypevalue AS treatmenttypevalue,
                        ah.authpriority_json AS authpriority,
                        rp.requestpriorityvalue AS requestpriorityvalue,

                        concat(md.firstname, ' ', md.lastname) AS membername

                    FROM public.authdetail ad

                    -- normalize JSON root (array[0] vs object)
                    LEFT JOIN LATERAL (
                        SELECT CASE
                            WHEN jsonb_typeof(ad.data::jsonb) = 'array' THEN ad.data::jsonb -> 0
                            ELSE ad.data::jsonb
                        END AS root
                    ) j ON TRUE

                    -- root-level fields (new save payload)
                    LEFT JOIN LATERAL (
                        SELECT
                            (j.root->>'treatmentType') AS treatmenttype_json,
                            COALESCE(j.root->>'requestPriority', j.root->>'requestSent') AS authpriority_json
                    ) ah ON TRUE

                    -- lookups
                    LEFT JOIN LATERAL (
                        SELECT elem->>'authStatus' AS authstatusvalue
                        FROM cfgadmindata cad,
                             jsonb_array_elements(cad.jsoncontent::jsonb->'authstatus') elem
                        WHERE (elem->>'id')::int = ad.authstatus
                          AND cad.module = 'UM'
                        LIMIT 1
                    ) rl ON TRUE

                    LEFT JOIN LATERAL (
                        SELECT elem->>'authClass' AS authclassvalue
                        FROM cfgadmindata cad,
                             jsonb_array_elements(cad.jsoncontent::jsonb->'authclass') elem
                        WHERE (elem->>'id')::int = ad.authclassid
                          AND cad.module = 'UM'
                        LIMIT 1
                    ) ac ON TRUE

                    LEFT JOIN LATERAL (
                        SELECT elem->>'requestPriority' AS requestpriorityvalue
                        FROM cfgadmindata cad,
                             jsonb_array_elements(cad.jsoncontent::jsonb->'requestpriority') elem
                        WHERE (elem->>'id')::text = ah.authpriority_json
                          AND cad.module = 'UM'
                        LIMIT 1
                    ) rp ON TRUE

                    LEFT JOIN LATERAL (
                        SELECT elem->>'treatmentType' AS treatmenttypevalue
                        FROM cfgadmindata cad,
                             jsonb_array_elements(cad.jsoncontent::jsonb->'treatmenttype') elem
                        WHERE (elem->>'id')::text = ah.treatmenttype_json
                          AND cad.module = 'UM'
                        LIMIT 1
                    ) tt ON TRUE

                    LEFT JOIN cfgauthtemplate at ON at.authtemplateid = ad.authtypeid
                    LEFT JOIN securityuser su ON su.userid = ad.createdby
                    LEFT JOIN memberdetails md ON md.memberdetailsid = ad.memberdetailsid

                    WHERE ad.deletedby IS NULL
                      AND ad.authassignedto = @userId
                    ORDER BY ad.createdon DESC;
                ";

            var results = new List<AuthDetailListItem>();

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            // Cache ordinals ONCE (also guarantees column names match)
            int o_authid = reader.GetOrdinal("authdetailid");
            int o_authnumber = reader.GetOrdinal("authnumber");
            int o_authstatus = reader.GetOrdinal("authstatus");
            int o_authstatusvalue = reader.GetOrdinal("authstatusvalue");
            int o_templatename = reader.GetOrdinal("templatename");
            int o_authtemplateid = reader.GetOrdinal("authtemplateid");
            int o_authclassvalue = reader.GetOrdinal("authclassvalue");
            int o_memberdetailsid = reader.GetOrdinal("memberdetailsid");
            int o_memberid = reader.GetOrdinal("memberid");
            int o_nextreviewdate = reader.GetOrdinal("nextreviewdate");
            int o_authduedate = reader.GetOrdinal("authduedate");
            int o_createdon = reader.GetOrdinal("createdon");
            int o_createdby = reader.GetOrdinal("createdby");
            int o_createduser = reader.GetOrdinal("createduser");
            int o_updatedon = reader.GetOrdinal("updatedon");
            int o_updatedby = reader.GetOrdinal("updatedby");
            int o_treatmenttype = reader.GetOrdinal("treatmenttype");
            int o_treatmenttypevalue = reader.GetOrdinal("treatmenttypevalue");
            int o_authpriority = reader.GetOrdinal("authpriority");
            int o_requestpriorityvalue = reader.GetOrdinal("requestpriorityvalue");
            int o_membername = reader.GetOrdinal("membername");

            while (await reader.ReadAsync())
            {
                var item = new AuthDetailListItem
                {
                    AuthDetailId = reader.IsDBNull(o_authid) ? (int?)null : reader.GetInt32(o_authid),
                    AuthNumber = reader.IsDBNull(o_authnumber) ? "" : reader.GetString(o_authnumber),

                    AuthStatus = reader.IsDBNull(o_authstatus) ? (int?)null : reader.GetInt32(o_authstatus),
                    AuthStatusValue = reader.IsDBNull(o_authstatusvalue) ? null : reader.GetString(o_authstatusvalue),

                    TemplateName = reader.IsDBNull(o_templatename) ? null : reader.GetString(o_templatename),
                    AuthtemplateId = reader.IsDBNull(o_authtemplateid) ? (int?)null : reader.GetInt32(o_authtemplateid),
                    AuthClassValue = reader.IsDBNull(o_authclassvalue) ? null : reader.GetString(o_authclassvalue),

                    MemberDetailsId = reader.IsDBNull(o_memberdetailsid) ? (int?)null : reader.GetInt32(o_memberdetailsid),
                    MemberId = reader.IsDBNull(o_memberid) ? 0 : reader.GetInt32(o_memberid),

                    NextReviewDate = reader.IsDBNull(o_nextreviewdate) ? (DateTime?)null : reader.GetDateTime(o_nextreviewdate),
                    AuthDueDate = reader.IsDBNull(o_authduedate) ? (DateTime?)null : reader.GetDateTime(o_authduedate),

                    CreatedOn = reader.GetDateTime(o_createdon),
                    CreatedBy = reader.IsDBNull(o_createdby) ? 0 : reader.GetInt32(o_createdby),
                    CreatedUser = reader.IsDBNull(o_createduser) ? null : reader.GetString(o_createduser),

                    UpdatedOn = reader.IsDBNull(o_updatedon) ? (DateTime?)null : reader.GetDateTime(o_updatedon),
                    UpdatedBy = reader.IsDBNull(o_updatedby) ? (int?)null : reader.GetInt32(o_updatedby),

                    TreatmentType = reader.IsDBNull(o_treatmenttype) ? null : reader.GetString(o_treatmenttype),
                    TreatmentTypeValue = reader.IsDBNull(o_treatmenttypevalue) ? null : reader.GetString(o_treatmenttypevalue),

                    AuthPriority = reader.IsDBNull(o_authpriority) ? null : reader.GetString(o_authpriority),
                    RequestPriorityValue = reader.IsDBNull(o_requestpriorityvalue) ? null : reader.GetString(o_requestpriorityvalue),

                    MemberName = reader.IsDBNull(o_membername) ? null : reader.GetString(o_membername)
                };

                results.Add(item);
            }

            return results;
        }


        public async Task<List<ActivityItem>> GetPendingActivitiesAsync(int? userId = null)
        {
            const string sql = @" SELECT * FROM (
                select distinct
                    'UM' as module,
                    md.firstname,
                    md.lastname,
                    md.memberid,
                    md.memberdetailsid,
                    aa.createdon,
                    aa.activitytypeid,
                    at.activitytype,
                    aa.referto,
                    su.username,
                    aa.followupdatetime,
                    aa.duedate,
                    aa.statusid,
                    'Pending' as status,
                    ad.authnumber,
                    aa.authactivityid as activityid
                from authactivity aa
                join authdetail ad on ad.authdetailid = aa.authdetailid
                join memberdetails md on md.memberdetailsid = ad.memberdetailsid
                join securityuser su on su.userid = aa.referto
                left join lateral (
                    select elem->>'activityType' as activitytype
                    from cfgadmindata cad,
                         jsonb_array_elements(cad.jsoncontent::jsonb->'activitytype') elem
                    where (elem->>'id')::int = aa.activitytypeid
                      and cad.module = 'UM'
                    limit 1
                ) at on true
                where aa.service_line_count = 0
                  and (@userId is null or aa.referto = @userId)

                UNION ALL

                --Member activities(no authnumber, but same ActivityItem shape)
                SELECT DISTINCT
                    'CM' AS module,
                    md.firstname,
                    md.lastname,
                    md.memberid,
                    md.memberdetailsid,
                    ma.createdon,
                    ma.activitytypeid,
                    at2.activitytype,
                    ma.referto,
                    su2.username,
                    ma.followupdatetime,
                    ma.duedate,
                    ma.statusid,
                    'Pending' AS status,
                    Null AS authnumber,
                    ma.memberactivityid as activityid
                FROM memberactivity ma
                JOIN memberdetails md ON md.memberdetailsid = ma.memberdetailsid
                JOIN securityuser su2 ON su2.userid = ma.referto
                LEFT JOIN LATERAL(
                    SELECT elem->> 'activityType' AS activitytype
                    FROM cfgadmindata cad,
                         jsonb_array_elements(cad.jsoncontent::jsonb->'activitytype') elem
                    WHERE(elem->> 'id')::int = ma.activitytypeid
                      AND cad.module = 'UM'
                    LIMIT 1
                ) at2 ON TRUE
                WHERE ma.referto IS NOT NULL
                  AND ma.deletedon IS NULL
                  AND COALESCE(ma.activeflag, TRUE) = TRUE
                  AND(@userId IS NULL OR ma.referto = @userId)
            ) allacts
            ORDER BY allacts.createdon DESC; ";

            var results = new List<ActivityItem>();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            if (userId.HasValue) cmd.Parameters.AddWithValue("@userId", userId.Value);
            else cmd.Parameters.AddWithValue("@userId", DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            while (await reader.ReadAsync())
            {
                var item = new ActivityItem
                {
                    Module = reader.IsDBNull(reader.GetOrdinal("module")) ? null : reader.GetString(reader.GetOrdinal("module")),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("firstname")) ? null : reader.GetString(reader.GetOrdinal("firstname")),
                    LastName = reader.IsDBNull(reader.GetOrdinal("lastname")) ? null : reader.GetString(reader.GetOrdinal("lastname")),
                    MemberId = reader.IsDBNull(reader.GetOrdinal("memberid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberid")),
                    MemberDetailsId = reader.IsDBNull(reader.GetOrdinal("memberdetailsid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberdetailsid")),
                    CreatedOn = reader.IsDBNull(reader.GetOrdinal("createdon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("createdon")),

                    ActivityTypeId = reader.IsDBNull(reader.GetOrdinal("activitytypeid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("activitytypeid")),
                    ActivityType = reader.IsDBNull(reader.GetOrdinal("activitytype")) ? null : reader.GetString(reader.GetOrdinal("activitytype")),

                    ReferredTo = reader.IsDBNull(reader.GetOrdinal("referto")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("referto")),
                    UserName = reader.IsDBNull(reader.GetOrdinal("username")) ? null : reader.GetString(reader.GetOrdinal("username")),

                    FollowUpDateTime = reader.IsDBNull(reader.GetOrdinal("followupdatetime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("followupdatetime")),
                    DueDate = reader.IsDBNull(reader.GetOrdinal("duedate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("duedate")),

                    StatusId = reader.IsDBNull(reader.GetOrdinal("statusid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("statusid")),
                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? null : reader.GetString(reader.GetOrdinal("status")),
                    AuthNumber = reader.IsDBNull(reader.GetOrdinal("authnumber")) ? null : reader.GetString(reader.GetOrdinal("authnumber")),
                    ActivityId = reader.IsDBNull(reader.GetOrdinal("activityid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("activityid"))
                };

                results.Add(item);
            }

            return results;
        }

        public async Task<List<ActivityRequestItem>> GetRequestActivitiesAsync(int? userId = null)
        {
            const string sql = @"
                    (
                -- =========================
                -- CM (your existing query)
                -- =========================
                SELECT distinct
                    'CM' AS module,
                    md.firstname,
                    md.lastname,
                    md.memberid,
                    md.memberdetailsid,
                    ma.createdon,
                    ma.activitytypeid,
                    at.activitytype,
                    NULL::int  AS referto,
                    NULL::text AS username,
                    ma.followupdatetime,
                    ma.duedate,
                    ma.statusid,
                    'Request' AS status,
                    NULL::text AS authnumber,
                    COALESCE(rj.rejectedcount, 0)                    AS rejectedcount,
                    COALESCE(rj.rejecteduserids, ARRAY[]::integer[]) AS rejecteduserids,
                    wg.workgroupid                                   AS workgroupid,
                    wg.workgroupname                                 AS workgroupname,
                    wb.workbasketid                                  AS workbasketid,
                    wb.workbasketname                                AS workbasketname,
                    maw.memberactivityworkgroupid                    AS memberactivityworkgroupid
                FROM cfguserworkgroup cug
                JOIN cfgworkgroupworkbasket cww
                    ON cww.workgroupworkbasketid = cug.workgroupworkbasketid
                JOIN cfgworkgroup wg
                    ON wg.workgroupid = cww.workgroupid
                JOIN cfgworkbasket wb
                    ON wb.workbasketid = cww.workbasketid
                JOIN memberactivityworkgroup maw
                    ON maw.workgroupworkbasketid = cug.workgroupworkbasketid
                JOIN memberactivity ma
                    ON ma.memberactivityid = maw.memberactivityid
                JOIN memberdetails md
                    ON md.memberdetailsid = ma.memberdetailsid
                LEFT JOIN LATERAL (
                    SELECT elem->>'activityType' AS activitytype
                    FROM cfgadmindata cad,
                         jsonb_array_elements(cad.jsoncontent::jsonb->'activitytype') elem
                    WHERE (elem->>'id')::int = ma.activitytypeid
                      AND cad.module = 'UM'
                    LIMIT 1
                ) at ON TRUE
                LEFT JOIN (
                    SELECT
                        memberactivityworkgroupid,
                        COUNT(*) FILTER (
                            WHERE upper(actiontype) IN ('REJECT','REJECTED')
                              AND COALESCE(activeflag, TRUE) = TRUE
                        ) AS rejectedcount,
                        ARRAY_AGG(userid) FILTER (
                            WHERE upper(actiontype) IN ('REJECT','REJECTED')
                              AND COALESCE(activeflag, TRUE) = TRUE
                        ) AS rejecteduserids
                    FROM memberactivityworkgroupaction
                    GROUP BY memberactivityworkgroupid
                ) rj
                  ON rj.memberactivityworkgroupid = maw.memberactivityworkgroupid
                WHERE COALESCE(cug.activeflag, TRUE) = TRUE
                  AND COALESCE(maw.activeflag, TRUE) = TRUE
                  -- AND (@userId IS NULL OR cug.userid = @userId)
                  AND ma.referto IS NULL
                  AND ma.isworkbasket = TRUE
                  AND ma.deletedon IS NULL
                  AND COALESCE(ma.activeflag, TRUE) = TRUE
            )

            UNION ALL

            (
                -- =========================
                -- UM (AUTH pending in WG/WB pool)
                -- =========================
                SELECT distinct
                    'UM' AS module,
                    md.firstname,
                    md.lastname,
                    md.memberid,
                    md.memberdetailsid,
                    a.createdon,
                    NULL::int AS activitytypeid,
                    NULL::text AS activitytype,
                    NULL::int  AS referto,
                    NULL::text AS username,
                    NULL::timestamp AS followupdatetime,
                    a.authduedate AS duedate,
                    a.authstatus  AS statusid,
                    'Request' AS status,
                    a.authnumber AS authnumber,
                    COALESCE(rj.rejectedcount, 0)                    AS rejectedcount,
                    COALESCE(rj.rejecteduserids, ARRAY[]::integer[]) AS rejecteduserids,
                    wg.workgroupid                                   AS workgroupid,
                    wg.workgroupname                                 AS workgroupname,
                    wb.workbasketid                                  AS workbasketid,
                    wb.workbasketname                                AS workbasketname,
                    awg.authworkgroupid                              AS memberactivityworkgroupid
                FROM cfguserworkgroup cug
                JOIN cfgworkgroupworkbasket cww
                    ON cww.workgroupworkbasketid = cug.workgroupworkbasketid
                JOIN cfgworkgroup wg
                    ON wg.workgroupid = cww.workgroupid
                JOIN cfgworkbasket wb
                    ON wb.workbasketid = cww.workbasketid
                JOIN authworkgroup awg
                    ON awg.workgroupworkbasketid = cug.workgroupworkbasketid
                   AND awg.requesttype = 'AUTH'
                   AND COALESCE(awg.activeflag, TRUE) = TRUE
                JOIN authdetail a
                    ON a.authdetailid = awg.authdetailid
                JOIN memberdetails md
                    ON md.memberdetailsid = a.memberdetailsid
                LEFT JOIN (
                    SELECT
                        authworkgroupid,
                        COUNT(*) FILTER (
                            WHERE upper(actiontype) IN ('REJECT','REJECTED')
                              AND COALESCE(activeflag, TRUE) = TRUE
                        ) AS rejectedcount,
                        ARRAY_AGG(userid) FILTER (
                            WHERE upper(actiontype) IN ('REJECT','REJECTED')
                              AND COALESCE(activeflag, TRUE) = TRUE
                        ) AS rejecteduserids
                    FROM authworkgroupaction
                    GROUP BY authworkgroupid
                ) rj
                  ON rj.authworkgroupid = awg.authworkgroupid
                WHERE COALESCE(cug.activeflag, TRUE) = TRUE
                  -- AND (@userId IS NULL OR cug.userid = @userId)
                  AND a.deletedon IS NULL
                  AND a.authassignedto IS NULL                -- still in pool
                  AND NOT EXISTS (
                      SELECT 1
                      FROM authworkgroupaction awa
                      WHERE awa.authworkgroupid = awg.authworkgroupid
                        AND COALESCE(awa.activeflag, TRUE) = TRUE
                        AND upper(awa.actiontype) IN ('ACCEPT','ACCEPTED')
                      LIMIT 1
                  )
            )

            UNION ALL

            (
                -- =========================
                -- AG (CASE pending in WG/WB pool)
                -- =========================
                -- NOTE: caseheader/memberdetails join columns aren’t in the provided tables snippet,
                -- so this assumes caseheader has memberdetailsid. If not, keep md.* as NULL.
                SELECT distinct
                    'AG' AS module,
                    md.firstname,
                    md.lastname,
                    md.memberid,
                    md.memberdetailsid,
                    cd.createdon,
                    NULL::int AS activitytypeid,
                    NULL::text AS activitytype,
                    NULL::int  AS referto,
                    NULL::text AS username,
                    NULL::timestamp AS followupdatetime,
                    NULL::timestamp AS duedate,
                    NULL::int AS statusid,
                    'Request' AS status,
                    ch.casenumber AS authnumber,
                    COALESCE(rj.rejectedcount, 0)                    AS rejectedcount,
                    COALESCE(rj.rejecteduserids, ARRAY[]::integer[]) AS rejecteduserids,
                    wg.workgroupid                                   AS workgroupid,
                    wg.workgroupname                                 AS workgroupname,
                    wb.workbasketid                                  AS workbasketid,
                    wb.workbasketname                                AS workbasketname,
                    cw.caseworkgroupid                               AS memberactivityworkgroupid
                FROM cfguserworkgroup cug
                JOIN cfgworkgroupworkbasket cww
                    ON cww.workgroupworkbasketid = cug.workgroupworkbasketid
                JOIN cfgworkgroup wg
                    ON wg.workgroupid = cww.workgroupid
                JOIN cfgworkbasket wb
                    ON wb.workbasketid = cww.workbasketid
                JOIN caseworkgroup cw
                    ON cw.workgroupworkbasketid = cug.workgroupworkbasketid
                   AND cw.requesttype = 'CASE'
                   AND COALESCE(cw.activeflag, TRUE) = TRUE
                LEFT JOIN casedetail cd
                    ON cd.caseheaderid = cw.caseheaderid
                   AND cd.caselevelid  = cw.caselevelid
                   AND cd.deletedon IS NULL
                LEFT JOIN caseheader ch
                    ON ch.caseheaderid = cw.caseheaderid
                LEFT JOIN memberdetails md
                    ON md.memberdetailsid = ch.memberdetailid
                LEFT JOIN (
                    SELECT
                        caseworkgroupid,
                        COUNT(*) FILTER (
                            WHERE upper(actiontype) IN ('REJECT','REJECTED')
                              AND COALESCE(activeflag, TRUE) = TRUE
                        ) AS rejectedcount,
                        ARRAY_AGG(userid) FILTER (
                            WHERE upper(actiontype) IN ('REJECT','REJECTED')
                              AND COALESCE(activeflag, TRUE) = TRUE
                        ) AS rejecteduserids
                    FROM caseworkgroupaction
                    GROUP BY caseworkgroupid
                ) rj
                  ON rj.caseworkgroupid = cw.caseworkgroupid
                WHERE COALESCE(cug.activeflag, TRUE) = TRUE
                  -- AND (@userId IS NULL OR cug.userid = @userId)
                  AND NOT EXISTS (
                      SELECT 1
                      FROM caseworkgroupaction cwa
                      WHERE cwa.caseworkgroupid = cw.caseworkgroupid
                        AND COALESCE(cwa.activeflag, TRUE) = TRUE
                        AND upper(cwa.actiontype) IN ('ACCEPT','ACCEPTED')
                      LIMIT 1
                  )
            )

            ORDER BY createdon DESC;";
            //cug.userid = @userId   AND
            var results = new List<ActivityRequestItem>();

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conn);
            if (userId.HasValue)
                cmd.Parameters.AddWithValue("@userId", userId.Value);
            else
                cmd.Parameters.AddWithValue("@userId", DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            while (await reader.ReadAsync())
            {
                var item = new ActivityRequestItem
                {
                    Module = reader.IsDBNull(reader.GetOrdinal("module")) ? null : reader.GetString(reader.GetOrdinal("module")),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("firstname")) ? null : reader.GetString(reader.GetOrdinal("firstname")),
                    LastName = reader.IsDBNull(reader.GetOrdinal("lastname")) ? null : reader.GetString(reader.GetOrdinal("lastname")),
                    MemberId = reader.IsDBNull(reader.GetOrdinal("memberid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberid")),
                    MemberDetailsId = reader.IsDBNull(reader.GetOrdinal("memberdetailsid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberdetailsid")),
                    CreatedOn = reader.IsDBNull(reader.GetOrdinal("createdon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("createdon")),
                    ActivityTypeId = reader.IsDBNull(reader.GetOrdinal("activitytypeid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("activitytypeid")),
                    ActivityType = reader.IsDBNull(reader.GetOrdinal("activitytype")) ? null : reader.GetString(reader.GetOrdinal("activitytype")),
                    // For request items, there is no assigned user yet
                    ReferredTo = reader.IsDBNull(reader.GetOrdinal("referto")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("referto")),
                    UserName = reader.IsDBNull(reader.GetOrdinal("username")) ? null : reader.GetString(reader.GetOrdinal("username")),
                    FollowUpDateTime = reader.IsDBNull(reader.GetOrdinal("followupdatetime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("followupdatetime")),
                    DueDate = reader.IsDBNull(reader.GetOrdinal("duedate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("duedate")),
                    StatusId = reader.IsDBNull(reader.GetOrdinal("statusid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("statusid")),
                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? null : reader.GetString(reader.GetOrdinal("status")),
                    AuthNumber = reader.IsDBNull(reader.GetOrdinal("authnumber")) ? null : reader.GetString(reader.GetOrdinal("authnumber")),
                    RejectedCount = reader.IsDBNull(reader.GetOrdinal("rejectedcount")) ? 0 : reader.GetInt32(reader.GetOrdinal("rejectedcount")),
                    RejectedUserIds = !reader.IsDBNull(reader.GetOrdinal("rejecteduserids")) ? reader.GetFieldValue<int[]>(reader.GetOrdinal("rejecteduserids")) : Array.Empty<int>(),
                    WorkGroupId = reader.IsDBNull(reader.GetOrdinal("workgroupid")) ? 0 : reader.GetInt32(reader.GetOrdinal("workgroupid")),
                    WorkGroupName = reader.IsDBNull(reader.GetOrdinal("workgroupname")) ? null : reader.GetString(reader.GetOrdinal("workgroupname")),
                    WorkBasketId = reader.IsDBNull(reader.GetOrdinal("workbasketid")) ? 0 : reader.GetInt32(reader.GetOrdinal("workbasketid")),
                    WorkBasketName = reader.IsDBNull(reader.GetOrdinal("workbasketname")) ? null : reader.GetString(reader.GetOrdinal("workbasketname")),
                    MemberActivityWorkGroupId = reader.IsDBNull(reader.GetOrdinal("memberactivityworkgroupid")) ? 0 : reader.GetInt32(reader.GetOrdinal("memberactivityworkgroupid"))
                };
                results.Add(item);
            }
            return results;
        }

        public async Task<List<UserWorkGroupWorkBasketItem>> GetUserWorkGroupWorkBasketsAsync(int userId)
        {
            const string sql = @"
                SELECT
                cug0.userid,
                su0.username,
                cug0.workgroupworkbasketid,
                wgw.workgroupid,
                wg.workgroupname,
                wgw.workbasketid,
                wb.workbasketname,
                COALESCE(cug0.activeflag, TRUE) AS activeflag,
                COALESCE(agg.assigneduserids, ARRAY[]::integer[])   AS assigneduserids,
                COALESCE(agg.assignedusernames, ARRAY[]::text[])    AS assignedusernames,
                wg.isfax
                    FROM cfguserworkgroup cug0
                    JOIN securityuser su0
                      ON su0.userid = cug0.userid
                    JOIN cfgworkgroupworkbasket wgw
                      ON wgw.workgroupworkbasketid = cug0.workgroupworkbasketid
                    JOIN cfgworkgroup wg
                      ON wg.workgroupid = wgw.workgroupid
                    JOIN cfgworkbasket wb
                      ON wb.workbasketid = wgw.workbasketid
                    LEFT JOIN (
                        SELECT
                            cug.workgroupworkbasketid,
                            ARRAY_AGG(cug.userid) FILTER (WHERE COALESCE(cug.activeflag, TRUE) = TRUE)
                                AS assigneduserids,
                            ARRAY_AGG(su.username) FILTER (WHERE COALESCE(cug.activeflag, TRUE) = TRUE)
                                AS assignedusernames
                        FROM cfguserworkgroup cug
                        JOIN securityuser su
                          ON su.userid = cug.userid
                        GROUP BY cug.workgroupworkbasketid
                    ) agg
                      ON agg.workgroupworkbasketid = cug0.workgroupworkbasketid
                    WHERE cug0.userid = @userId
                      AND COALESCE(cug0.activeflag, TRUE) = TRUE
                      AND COALESCE(wgw.activeflag, TRUE) = TRUE
                      AND COALESCE(wg.activeflag, TRUE) = TRUE
                      AND COALESCE(wb.activeflag, TRUE) = TRUE
                    ORDER BY wg.workgroupname, wb.workbasketname;";

            var results = new List<UserWorkGroupWorkBasketItem>();

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            while (await reader.ReadAsync())
            {
                var item = new UserWorkGroupWorkBasketItem
                {
                    UserId = reader.IsDBNull(reader.GetOrdinal("userid")) ? 0 : reader.GetInt32(reader.GetOrdinal("userid")),
                    UserFullName = reader.IsDBNull(reader.GetOrdinal("username")) ? null : reader.GetString(reader.GetOrdinal("username")),
                    WorkGroupWorkBasketId = reader.IsDBNull(reader.GetOrdinal("workgroupworkbasketid")) ? 0 : reader.GetInt32(reader.GetOrdinal("workgroupworkbasketid")),
                    WorkGroupId = reader.IsDBNull(reader.GetOrdinal("workgroupid")) ? 0 : reader.GetInt32(reader.GetOrdinal("workgroupid")),
                    WorkGroupName = reader.IsDBNull(reader.GetOrdinal("workgroupname")) ? null : reader.GetString(reader.GetOrdinal("workgroupname")),
                    WorkBasketId = reader.IsDBNull(reader.GetOrdinal("workbasketid")) ? 0 : reader.GetInt32(reader.GetOrdinal("workbasketid")),
                    WorkBasketName = reader.IsDBNull(reader.GetOrdinal("workbasketname")) ? null : reader.GetString(reader.GetOrdinal("workbasketname")),
                    ActiveFlag = !reader.IsDBNull(reader.GetOrdinal("activeflag")) && reader.GetBoolean(reader.GetOrdinal("activeflag")),
                    AssignedUserIds = !reader.IsDBNull(reader.GetOrdinal("assigneduserids")) ? reader.GetFieldValue<int[]>(reader.GetOrdinal("assigneduserids")) : Array.Empty<int>(),
                    AssignedUserNames = !reader.IsDBNull(reader.GetOrdinal("assignedusernames")) ? reader.GetFieldValue<string[]>(reader.GetOrdinal("assignedusernames")) : Array.Empty<string>(),
                    IsFax = !reader.IsDBNull(reader.GetOrdinal("isfax")) && reader.GetBoolean(reader.GetOrdinal("isfax"))
                };

                results.Add(item);
            }

            return results;
        }


        public async Task<List<ActivityItem>> GetPendingWQAsync(int? userId = null)
        {
            const string sql = @"
                select distinct
                    'UM' as module,
                    md.firstname,
                    md.lastname,
                    md.memberid,
                    md.memberdetailsid,
                    aa.createdon,
                    aa.activitytypeid,
                    at.activitytype,
                    aa.referto,
                    su.username,
                    aa.followupdatetime,
                    aa.duedate,
                    aa.statusid,
                    md_review_status as status,
                    ad.authnumber,
                    aa.comment,
                    aa.authactivityid
                from authactivity aa
                join authdetail ad on ad.authdetailid = aa.authdetailid
                join memberdetails md on md.memberdetailsid = ad.memberdetailsid
                join securityuser su on su.userid = aa.referto
                left join lateral (
                    select elem->>'activityType' as activitytype
                    from cfgadmindata cad,
                         jsonb_array_elements(cad.jsoncontent::jsonb->'activitytype') elem
                    where (elem->>'id')::int = aa.activitytypeid
                      and cad.module = 'UM'
                    limit 1
                ) at on true
                where aa.service_line_count <> 0
                  and (@userId is null or aa.referto = @userId)
                order by aa.createdon desc;";

            var results = new List<ActivityItem>();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            if (userId.HasValue) cmd.Parameters.AddWithValue("@userId", userId.Value);
            else cmd.Parameters.AddWithValue("@userId", DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            while (await reader.ReadAsync())
            {
                var item = new ActivityItem
                {
                    Module = reader.IsDBNull(reader.GetOrdinal("module")) ? null : reader.GetString(reader.GetOrdinal("module")),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("firstname")) ? null : reader.GetString(reader.GetOrdinal("firstname")),
                    LastName = reader.IsDBNull(reader.GetOrdinal("lastname")) ? null : reader.GetString(reader.GetOrdinal("lastname")),
                    MemberId = reader.IsDBNull(reader.GetOrdinal("memberid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberid")),
                    MemberDetailsId = reader.IsDBNull(reader.GetOrdinal("memberdetailsid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberdetailsid")),
                    CreatedOn = reader.IsDBNull(reader.GetOrdinal("createdon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("createdon")),

                    ActivityTypeId = reader.IsDBNull(reader.GetOrdinal("activitytypeid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("activitytypeid")),
                    ActivityType = reader.IsDBNull(reader.GetOrdinal("activitytype")) ? null : reader.GetString(reader.GetOrdinal("activitytype")),

                    ReferredTo = reader.IsDBNull(reader.GetOrdinal("referto")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("referto")),
                    UserName = reader.IsDBNull(reader.GetOrdinal("username")) ? null : reader.GetString(reader.GetOrdinal("username")),

                    FollowUpDateTime = reader.IsDBNull(reader.GetOrdinal("followupdatetime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("followupdatetime")),
                    DueDate = reader.IsDBNull(reader.GetOrdinal("duedate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("duedate")),

                    StatusId = reader.IsDBNull(reader.GetOrdinal("statusid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("statusid")),
                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? null : reader.GetString(reader.GetOrdinal("status")),
                    AuthNumber = reader.IsDBNull(reader.GetOrdinal("authnumber")) ? null : reader.GetString(reader.GetOrdinal("authnumber")),
                    Comments = reader.IsDBNull(reader.GetOrdinal("comment")) ? null : reader.GetString(reader.GetOrdinal("comment")),
                    AuthActivityId = reader.IsDBNull(reader.GetOrdinal("authactivityid")) ? null : reader.GetInt32(reader.GetOrdinal("authactivityid")).ToString()
                };

                results.Add(item);
            }

            return results;
        }

        public async Task<List<AuthActivityLine>> GetWQActivityLines(int? activityid = null)
        {
            const string sql = @"
                select
                id, activityid, decisionlineid, servicecode, description,
                fromdate, todate, requested, approved, denied,
                initialrecommendation, status, mddecision, mdnotes,
                reviewedbyuserid, reviewedon, aal.updatedon, version, aa.comment
                from authactivityline aal
				JOIN authactivity aa on aa.authactivityid = aal.activityid
                where activityid = @activityid
                order by fromdate nulls first, id;";

            var results = new List<AuthActivityLine>();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            if (activityid.HasValue) cmd.Parameters.AddWithValue("@activityid", activityid.Value);
            else cmd.Parameters.AddWithValue("@activityid", DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            while (await reader.ReadAsync())
            {
                var item = new AuthActivityLine
                {
                    Id = reader.IsDBNull(reader.GetOrdinal("id")) ? 0 : reader.GetInt32(reader.GetOrdinal("id")),
                    ActivityId = reader.IsDBNull(reader.GetOrdinal("activityid")) ? 0 : reader.GetInt32(reader.GetOrdinal("activityid")),
                    DecisionLineId = reader.IsDBNull(reader.GetOrdinal("decisionlineid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("decisionlineid")),
                    ServiceCode = reader.IsDBNull(reader.GetOrdinal("servicecode")) ? string.Empty : reader.GetString(reader.GetOrdinal("servicecode")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? string.Empty : reader.GetString(reader.GetOrdinal("description")),
                    FromDate = reader.IsDBNull(reader.GetOrdinal("fromdate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("fromdate")),
                    ToDate = reader.IsDBNull(reader.GetOrdinal("todate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("todate")),
                    Requested = reader.IsDBNull(reader.GetOrdinal("requested")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("requested")),
                    Approved = reader.IsDBNull(reader.GetOrdinal("approved")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("approved")),
                    Denied = reader.IsDBNull(reader.GetOrdinal("denied")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("denied")),
                    InitialRecommendation = reader.IsDBNull(reader.GetOrdinal("initialrecommendation")) ? null : reader.GetString(reader.GetOrdinal("initialrecommendation")),
                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? string.Empty : reader.GetString(reader.GetOrdinal("status")),
                    MdDecision = reader.IsDBNull(reader.GetOrdinal("mddecision")) ? string.Empty : reader.GetString(reader.GetOrdinal("mddecision")),
                    MdNotes = reader.IsDBNull(reader.GetOrdinal("mdnotes")) ? null : reader.GetString(reader.GetOrdinal("mdnotes")),
                    ReviewedByUserId = reader.IsDBNull(reader.GetOrdinal("reviewedbyuserid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("reviewedbyuserid")),
                    ReviewedOn = reader.IsDBNull(reader.GetOrdinal("reviewedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("reviewedon")),
                    UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updatedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("updatedon")),
                    Version = reader.IsDBNull(reader.GetOrdinal("version")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("version")),
                    Comments = reader.IsDBNull(reader.GetOrdinal("comment")) ? null : reader.GetString(reader.GetOrdinal("comment"))
                };

                results.Add(item);
            }

            return results;
        }

        public async Task<int> UpdateAuthActivityLinesAsync(
    IEnumerable<int> lineIds,
    string status,
    string mdDecision,
    string? mdNotes,
    int reviewedByUserId)
        {
            if (lineIds == null) return 0;

            var idArray = lineIds.Distinct().ToArray();
            if (idArray.Length == 0) return 0;

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                const string updateSql = @"
            UPDATE authactivityline
            SET status           = @status,
                mddecision       = @mdDecision,
                mdnotes          = @mdNotes,
                reviewedbyuserid = @reviewedByUserId,
                reviewedon       = CASE WHEN @status = 'Completed' THEN NOW() ELSE reviewedon END,
                updatedon        = NOW()
            WHERE id = ANY(@ids);";

                await using var cmd = new NpgsqlCommand(updateSql, conn, tx);

                cmd.Parameters.AddWithValue("status", status);
                cmd.Parameters.AddWithValue("mdDecision", mdDecision);
                cmd.Parameters.AddWithValue("mdNotes", (object?)mdNotes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("reviewedByUserId", reviewedByUserId);

                var idsParam = cmd.Parameters.Add("ids", NpgsqlDbType.Array | NpgsqlDbType.Integer);
                idsParam.Value = idArray;

                var affected = await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"Update affected rows = {affected}");

                if (affected == 0)
                {
                    await tx.RollbackAsync();
                    return 0;
                }

                // Recompute rollups for all activities touched by these lines
                const string rollupSql = @"
            WITH affected_activities AS (
                SELECT DISTINCT activityid
                FROM authactivityline
                WHERE id = ANY(@ids)
            ),
            agg AS (
                SELECT
                    l.activityid,
                    COUNT(*) AS total,
                    COUNT(*) FILTER (WHERE l.status = 'Completed') AS completed,
                    COUNT(*) FILTER (WHERE l.status = 'Completed' AND l.mddecision = 'Approved') AS approved,
                    COUNT(*) FILTER (WHERE l.status = 'Completed' AND l.mddecision = 'Denied')   AS denied,
                    COUNT(*) FILTER (WHERE l.status = 'Completed' AND l.mddecision = 'Partial') AS partial
                FROM authactivityline l
                INNER JOIN affected_activities a ON a.activityid = l.activityid
                GROUP BY l.activityid
            )
            UPDATE authactivity a
            SET service_line_count    = agg.total,
                md_review_status      = CASE
                                           WHEN agg.completed = 0 THEN 'Pending'
                                           WHEN agg.completed < agg.total THEN 'InProgress'
                                           WHEN agg.approved = agg.total THEN 'Approved'
                                           WHEN agg.denied   = agg.total THEN 'Denied'                            
                                           ELSE 'Completed'
                                        END,
                md_aggregate_decision = CASE
                                           WHEN agg.completed = 0 THEN 'Pending'
                                           WHEN agg.completed < agg.total THEN 'InProgress'
                                           WHEN agg.approved = agg.total THEN 'Approved'
                                           WHEN agg.denied   = agg.total THEN 'Denied'
                                           ELSE 'Mixed'
                                        END
            FROM agg
            WHERE a.authactivityid = agg.activityid;";

                await using var roll = new NpgsqlCommand(rollupSql, conn, tx);
                var rollIds = roll.Parameters.Add("ids", NpgsqlDbType.Array | NpgsqlDbType.Integer);
                rollIds.Value = idArray;

                await roll.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                return affected;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }



        public async Task<long> InsertFaxFileAsync(FaxFile fax)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = @"

                INSERT INTO faxfiles
                (
                    filename, storedpath, originalname, contenttype, sizebytes, sha256hex,
                    receivedat, uploadedby, uploadedat,
                    pagecount, memberid, workbasket, priority, status, processstatus,
                    meta, ocrtext, ocrjsonpath, filebytes,
                    createdon, createdby, updatedon, updatedby, parentfaxid
                )
                VALUES
                (
                    @filename, @storedpath, @originalname, @contenttype, @sizebytes, @sha256hex,
                    @receivedat, @uploadedby, @uploadedat,
                    @pagecount, @memberid, @workbasket, @priority, @status, @processstatus,
                    @meta, @ocrtext, @ocrjsonpath, @filebytes,
                    @createdon, @createdby, @updatedon, @updatedby, @parentfaxid
                )
                RETURNING faxid;";

            using var cmd = new NpgsqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@filename", fax.FileName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@storedpath", fax.Url ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@originalname", (object?)fax.OriginalName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@contenttype", (object?)fax.ContentType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sizebytes", (object?)fax.SizeBytes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sha256hex", (object?)fax.Sha256Hex ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@receivedat", fax.ReceivedAt);
            cmd.Parameters.AddWithValue("@uploadedby", (object?)fax.UploadedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@uploadedat", (object?)fax.UploadedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@filebytes", (object?)fax.FileBytes ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@pagecount", fax.PageCount);
            cmd.Parameters.AddWithValue("@memberid", (object?)fax.MemberId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@workbasket", (object?)fax.WorkBasket ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@priority", fax.Priority);
            cmd.Parameters.AddWithValue("@status", fax.Status ?? "New");
            cmd.Parameters.AddWithValue("@processstatus", fax.ProcessStatus ?? "Pending");
            cmd.Parameters.AddWithValue("@parentfaxid", fax.ParentFaxId);

            // meta jsonb
            if (string.IsNullOrWhiteSpace(fax.MetaJson))
                cmd.Parameters.AddWithValue("@meta", NpgsqlDbType.Jsonb, DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@meta", NpgsqlDbType.Jsonb, fax.MetaJson);

            cmd.Parameters.AddWithValue("@ocrtext", (object?)fax.OcrText ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ocrjsonpath", (object?)fax.OcrJsonPath ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@createdon", fax.CreatedOn);
            cmd.Parameters.AddWithValue("@createdby", (object?)fax.CreatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updatedon", (object?)fax.UpdatedOn ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updatedby", (object?)fax.UpdatedBy ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return (long)result!;
        }


        public async Task<int> UpdateFaxFileAsync(FaxFile fax)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = @"
                UPDATE faxfiles
                SET
                    filename      = @filename,
                    memberid      = @memberid,
                    workbasket    = @workbasket,
                    priority      = @priority,
                    status        = @status,
                    processstatus = @processstatus,
                    updatedon     = @updatedon,
                    updatedby     = @updatedby,
                    deletedon     = @deletedon,
                    deletedby     = @deletedby
                WHERE faxid = @faxid;";

            using var cmd = new NpgsqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@faxid", fax.FaxId);
            cmd.Parameters.AddWithValue("@filename", fax.FileName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@memberid", (object?)fax.MemberId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@workbasket", (object?)fax.WorkBasket ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@priority", fax.Priority);
            cmd.Parameters.AddWithValue("@status", fax.Status ?? "New");
            cmd.Parameters.AddWithValue("@processstatus", fax.ProcessStatus ?? "Pending");
            cmd.Parameters.AddWithValue("@updatedon", fax.UpdatedOn ?? DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@updatedby", (object?)fax.UpdatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@deletedon", fax.DeletedOn ?? DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@deletedby", (object?)fax.DeletedBy ?? DBNull.Value);

            return await cmd.ExecuteNonQueryAsync();
        }



        public async Task<(List<FaxFile> Items, int Total)> GetFaxFilesAsync(
            string? search, int page, int pageSize, string? status)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 25;

            var results = new List<FaxFile>();
            var total = 0;

            const string sql = @"
              SELECT
                  f.faxid,
                  f.filename,
                  f.storedpath,
                  f.originalname,
                  f.contenttype,
                  f.sizebytes,
                  f.sha256hex,
                  f.receivedat,
                  f.uploadedby,
                  f.uploadedat,
                  f.pagecount,
                  f.memberid,
                  wb.workbasketname as workbasket,
                  f.priority,
                  f.status,
                  f.processstatus,
                  f.meta,
                  f.ocrtext,
                  f.ocrjsonpath,
                  f.createdon,
                  f.createdby,
                  f.updatedon,
                  f.updatedby,
                  f.parentfaxid,
                  COUNT(*) OVER() AS total_count
              FROM faxfiles f
			  LEFT JOIN cfgworkbasket wb on f.workbasket::integer = wb.workbasketid
              where f.deletedby is null
              ORDER BY f.receivedat DESC;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (total == 0 && !reader.IsDBNull(reader.GetOrdinal("total_count")))
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));

                var f = new FaxFile
                {
                    FaxId = reader.GetInt64(reader.GetOrdinal("faxid")),
                    FileName = reader.GetString(reader.GetOrdinal("filename")),
                    Url = reader.IsDBNull(reader.GetOrdinal("storedpath")) ? "" : reader.GetString(reader.GetOrdinal("storedpath")),
                    OriginalName = reader.IsDBNull(reader.GetOrdinal("originalname")) ? null : reader.GetString(reader.GetOrdinal("originalname")),
                    ContentType = reader.IsDBNull(reader.GetOrdinal("contenttype")) ? null : reader.GetString(reader.GetOrdinal("contenttype")),
                    SizeBytes = reader.IsDBNull(reader.GetOrdinal("sizebytes")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("sizebytes")),
                    Sha256Hex = reader.IsDBNull(reader.GetOrdinal("sha256hex")) ? null : reader.GetString(reader.GetOrdinal("sha256hex")),
                    ReceivedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("receivedat")),
                    UploadedBy = reader.IsDBNull(reader.GetOrdinal("uploadedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("uploadedby")),
                    UploadedAt = reader.IsDBNull(reader.GetOrdinal("uploadedat")) ? (DateTimeOffset?)null : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("uploadedat")),
                    PageCount = reader.GetInt32(reader.GetOrdinal("pagecount")),
                    MemberId = reader.IsDBNull(reader.GetOrdinal("memberid")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("memberid")),
                    WorkBasket = reader.IsDBNull(reader.GetOrdinal("workbasket")) ? null : reader.GetString(reader.GetOrdinal("workbasket")),
                    Priority = reader.IsDBNull(reader.GetOrdinal("priority")) ? (short)2 : reader.GetInt16(reader.GetOrdinal("priority")),
                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "New" : reader.GetString(reader.GetOrdinal("status")),
                    ProcessStatus = reader.IsDBNull(reader.GetOrdinal("processstatus")) ? "Pending" : reader.GetString(reader.GetOrdinal("processstatus")),
                    MetaJson = reader.IsDBNull(reader.GetOrdinal("meta")) ? null : reader.GetString(reader.GetOrdinal("meta")),
                    OcrText = reader.IsDBNull(reader.GetOrdinal("ocrtext")) ? null : reader.GetString(reader.GetOrdinal("ocrtext")),
                    OcrJsonPath = reader.IsDBNull(reader.GetOrdinal("ocrjsonpath")) ? null : reader.GetString(reader.GetOrdinal("ocrjsonpath")),
                    CreatedOn = reader.GetDateTime(reader.GetOrdinal("createdon")),
                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("createdby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("createdby")),
                    UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updatedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("updatedon")),
                    UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updatedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("updatedby")),
                    ParentFaxId = reader.IsDBNull(reader.GetOrdinal("parentfaxid")) ? (int?)0 : reader.GetInt32(reader.GetOrdinal("parentfaxid"))
                };

                results.Add(f);
            }

            return (results, total);
        }

        public async Task<FaxFile?> GetFaxFileByIdAsync(long faxId)
        {
            const string sql = @"
                SELECT
                    faxid, filename, storedpath, originalname, contenttype, sizebytes, sha256hex,
                    receivedat, uploadedby, uploadedat, pagecount, memberid, workbasket,
                    priority, status, processstatus, meta, ocrtext, ocrjsonpath, filebytes,
                    createdon, createdby, updatedon, updatedby, parentfaxid
                FROM faxfiles
                WHERE faxid = @faxid;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@faxid", faxId);

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (await reader.ReadAsync())
            {
                return new FaxFile
                {
                    FaxId = reader.GetInt64(reader.GetOrdinal("faxid")),
                    FileName = reader.GetString(reader.GetOrdinal("filename")),
                    Url = reader.IsDBNull(reader.GetOrdinal("storedpath")) ? "" : reader.GetString(reader.GetOrdinal("storedpath")),
                    ReceivedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("receivedat")),
                    PageCount = reader.GetInt32(reader.GetOrdinal("pagecount")),
                    MemberId = reader.IsDBNull(reader.GetOrdinal("memberid")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("memberid")),
                    WorkBasket = reader.IsDBNull(reader.GetOrdinal("workbasket")) ? null : reader.GetString(reader.GetOrdinal("workbasket")),
                    Priority = reader.IsDBNull(reader.GetOrdinal("priority")) ? (short)2 : reader.GetInt16(reader.GetOrdinal("priority")),
                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "New" : reader.GetString(reader.GetOrdinal("status")),
                    FileBytes = reader.IsDBNull(reader.GetOrdinal("filebytes")) ? Array.Empty<byte>() : (byte[])reader["filebytes"],
                    CreatedOn = reader.GetDateTime(reader.GetOrdinal("createdon")),
                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("createdby")) ? 0 : reader.GetInt32(reader.GetOrdinal("createdby")),
                    UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updatedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("updatedon")),
                    UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updatedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("updatedby")),
                    ParentFaxId = reader.IsDBNull(reader.GetOrdinal("parentfaxid")) ? (int?)0 : reader.GetInt32(reader.GetOrdinal("parentfaxid"))
                };
            }

            return null;
        }

        public async Task<int> EndMemberCareStaffAsync(int memberDetailsId, DateTime endDate, int? careStaffId = null, int? updatedBy = null, CancellationToken ct = default)
        {
            const string sql = @"
                    WITH target AS (
                        SELECT mcs.membercarestaffid,
                               GREATEST(@endDate::date, mcs.startdate)::date AS new_end
                        FROM public.membercarestaff mcs
                        WHERE mcs.memberdetailsid = @memberDetailsId
                          AND (@careStaffId IS NULL OR mcs.userid = @careStaffId)
                          AND COALESCE(mcs.activeflag, TRUE) = TRUE
                          AND mcs.startdate <= @endDate::date
                          AND (mcs.enddate IS NULL OR mcs.enddate > @endDate::date)
                    ),
                    updated AS (
                        UPDATE public.membercarestaff m
                        SET enddate    = t.new_end,
                            activeflag = FALSE,
                            updatedon  = NOW(),
                            updatedby  = @updatedBy
                        FROM target t
                        WHERE m.membercarestaffid = t.membercarestaffid
                        RETURNING 1
                    )
                    SELECT COUNT(*) AS affected FROM updated;";

            //await using var conn = GetConnection();
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                sql,
                new
                {
                    memberDetailsId,
                    careStaffId,
                    endDate = endDate.Date,
                    updatedBy
                },
                cancellationToken: ct));
        }


        public async Task<IEnumerable<MemberSearchResultDto>> SearchMembersAsync(MemberSearchCriteriaDto criteria)
        {
            var sql = @"
                           SELECT
                        md.memberdetailsid       AS MemberDetailsId,
                        md.memberid              AS MemberId,
                        md.memberid::text        AS MedicaidId,
                        md.firstname             AS FirstName,
                        md.lastname              AS LastName,
                        md.birthdate             AS Dob,
                        NULL                     AS Gender,
                        md.activeflag::bool      AS Status,
                        ph.phone_number          AS PhoneNumber,
                        em.email_address         AS EmailAddress,
                        ad.addressline1          AS AddressLine1,
                        ad.city                  AS City,
                        NULL                     AS State,
                        ad.zipcode               AS ZipCode
                    FROM public.memberdetails md

                    -- pick ONE phone for display
                    LEFT JOIN LATERAL (
                        SELECT p.phonenumber AS phone_number
                        FROM public.memberphonenumber p
                        WHERE p.memberdetailsid = md.memberdetailsid
                        ORDER BY COALESCE(p.isprimary, false) DESC,
                                 COALESCE(p.createdon, now()) DESC
                        LIMIT 1
                    ) ph ON TRUE

                    -- pick ONE email for display
                    LEFT JOIN LATERAL (
                        SELECT NULL AS email_address
                        FROM public.memberemail e
                        WHERE e.memberdetailsid = md.memberdetailsid
                        ORDER BY COALESCE(e.isprimary, false) DESC,
                                 COALESCE(e.createdon, now()) DESC
                        LIMIT 1
                    ) em ON TRUE

                    -- pick ONE address for display
                    LEFT JOIN LATERAL (
                        SELECT
                            a.addressline1 AS addressline1,
                            a.city,
                            a.zipcode
                        FROM public.memberaddress a
                        WHERE a.memberdetailsid = md.memberdetailsid
                        ORDER BY COALESCE(a.isprimary, false) DESC,
                                 COALESCE(a.createdon, now()) DESC
                        LIMIT 1
                    ) ad ON TRUE

                    WHERE
                        (
                            @p_quicktext IS NULL
                            OR @p_quicktext = ''

                            OR md.memberid::text = @p_quicktext
                            OR (md.firstname || ' ' || md.lastname) ILIKE '%' || @p_quicktext || '%'
                            OR md.firstname ILIKE '%' || @p_quicktext || '%'
                            OR md.lastname  ILIKE '%' || @p_quicktext || '%'

                            -- any phone (only if quickText has digits)
                            OR (
                                @p_quicktext ~ '\d'
                                AND EXISTS (
                                    SELECT 1
                                    FROM public.memberphonenumber p
                                    WHERE p.memberdetailsid = md.memberdetailsid
                                      AND regexp_replace(COALESCE(p.phonenumber, ''), '\D', '', 'g')
                                          LIKE  regexp_replace(@p_quicktext, '\D', '', 'g') || '%'
                                )
                            )

                            -- any email
                            OR EXISTS (
                                SELECT 1
                                FROM public.memberemail e
                                WHERE e.memberdetailsid = md.memberdetailsid
                                  AND e.emailaddress ILIKE '%' || @p_quicktext || '%'
                            )

                            -- any address (addressline1 + city + zip)
                            OR EXISTS (
                                SELECT 1
                                FROM public.memberaddress a
                                WHERE a.memberdetailsid = md.memberdetailsid
                                  AND (
                                      (COALESCE(a.addressline1, '') || ' ' ||
                                       COALESCE(a.city, '')        || ' ' ||
                                       COALESCE(a.zipcode, '')
                                      ) ILIKE '%' || @p_quicktext || '%'
                                  )
                            )
                        )

                        -- ADVANCED FILTERS
                        AND (@p_firstname IS NULL OR md.firstname ILIKE @p_firstname || '%')
                        AND (@p_lastname  IS NULL OR md.lastname  ILIKE @p_lastname  || '%')

                        AND (@p_memberid   IS NULL OR md.memberid::text = @p_memberid)
                        AND (@p_medicaidid IS NULL OR md.memberid::text = @p_medicaidid)

                       -- AND (@p_dob_from IS NULL OR md.birthdate >= @p_dob_from::date)
                        --AND (@p_dob_to   IS NULL OR md.birthdate <= @p_dob_to::date)

                        -- phone filter (any phone)
                        AND (
                            @p_phone IS NULL
                            OR EXISTS (
                                SELECT 1
                                FROM public.memberphonenumber p2
                                WHERE p2.memberdetailsid = md.memberdetailsid
                                  AND regexp_replace(COALESCE(p2.phonenumber, ''), '\D', '', 'g')
                                      LIKE '%' || regexp_replace(@p_phone, '\D', '', 'g') || '%'
                            )
                        )

                        -- email filter (any email)
                        AND (
                            @p_email IS NULL
                            OR EXISTS (
                                SELECT 1
                                FROM public.memberemail e2
                                WHERE e2.memberdetailsid = md.memberdetailsid
                                  AND e2.emailaddress ILIKE '%' || @p_email || '%'
                            )
                        )

                        -- city filter
                        AND (
                            @p_city IS NULL
                            OR EXISTS (
                                SELECT 1
                                FROM public.memberaddress a2
                                WHERE a2.memberdetailsid = md.memberdetailsid
                                  AND a2.city ILIKE @p_city || '%'
                            )
                        )

                        -- zip filter
                        AND (
                            @p_zip IS NULL
                            OR EXISTS (
                                SELECT 1
                                FROM public.memberaddress a4
                                WHERE a4.memberdetailsid = md.memberdetailsid
                                  AND a4.zipcode LIKE @p_zip || '%'
                            )
                        )

                    ORDER BY md.lastname, md.firstname, md.memberid
                    LIMIT @Limit OFFSET @Offset; ";


            var offset = (criteria.PageNumber <= 1 ? 0 : (criteria.PageNumber - 1) * criteria.PageSize);

            await using var conn = new NpgsqlConnection(_connectionString);

            //Console.WriteLine("========= MEMBER SEARCH SQL =========");
            //Console.WriteLine(sql);
            //Console.WriteLine("=====================================");
            //Console.WriteLine("========= MEMBER SEARCH PARAMS =========");
            //Console.WriteLine($"QuickText   = {criteria.QuickText}");
            //Console.WriteLine($"FirstName   = {criteria.FirstName}");
            //Console.WriteLine($"LastName    = {criteria.LastName}");
            //Console.WriteLine($"MemberId    = {criteria.MemberId}");
            //Console.WriteLine($"MedicaidId  = {criteria.MedicaidId}");
            //Console.WriteLine($"DobFrom     = {criteria.DobFrom}");
            //Console.WriteLine($"DobTo       = {criteria.DobTo}");
            //Console.WriteLine($"Phone       = {criteria.Phone}");
            //Console.WriteLine($"Email       = {criteria.Email}");
            //Console.WriteLine($"City        = {criteria.City}");
            //Console.WriteLine($"State       = {criteria.State}");
            //Console.WriteLine($"Zip         = {criteria.Zip}");
            //Console.WriteLine($"Limit       = {criteria.PageSize}");
            //Console.WriteLine($"Offset      = {offset}");
            //Console.WriteLine("========================================");

            var results = await conn.QueryAsync<MemberSearchResultDto>(sql, new
            {
                p_quicktext = EmptyToNull(criteria.QuickText),

                p_firstname = EmptyToNull(criteria.FirstName),
                p_lastname = EmptyToNull(criteria.LastName),
                p_memberid = EmptyToNull(criteria.MemberId),
                p_medicaidid = EmptyToNull(criteria.MedicaidId),

                p_dob_from = criteria.DobFrom,
                p_dob_to = criteria.DobTo,

                // p_gender = EmptyToNull(criteria.Gender),
                p_phone = EmptyToNull(criteria.Phone),
                p_email = EmptyToNull(criteria.Email),

                p_city = EmptyToNull(criteria.City),
                //p_state = EmptyToNull(criteria.State),
                p_zip = EmptyToNull(criteria.Zip),

                Limit = criteria.PageSize,
                Offset = offset
            });

            return results;
        }

        private static string? EmptyToNull(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        public async Task<IReadOnlyList<AgCaseRow>> GetAgCasesAsync(int userId, CancellationToken ct = default)
        {
            const string sql = @"
                                WITH admin AS (
                                  SELECT jsoncontent::jsonb AS j
                                  FROM cfgadmindata
                                  WHERE module = 'AG'
                                  ORDER BY COALESCE(updatedon, createdon) DESC NULLS LAST
                                  LIMIT 1
                                ),
                                casetype_lu AS (
                                  SELECT (x ->> 'id') AS id, (x ->> 'caseType') AS casetype_name
                                  FROM admin
                                  CROSS JOIN LATERAL jsonb_array_elements(admin.j -> 'casetype') x
                                ),
                                casestatus_lu AS (
                                  SELECT (x ->> 'id') AS id, (x ->> 'caseStatus') AS casestatus_name
                                  FROM admin
                                  CROSS JOIN LATERAL jsonb_array_elements(admin.j -> 'casestatus') x
                                ),
                                casepriority_lu AS (
                                  SELECT (x ->> 'id') AS id, (x ->> 'casePriority') AS casepriority_name
                                  FROM admin
                                  CROSS JOIN LATERAL jsonb_array_elements(admin.j -> 'casepriority') x
                                )
                                SELECT
                                  ch.casenumber                                  AS ""CaseNumber"",
                                  ch.caseheaderid                               AS ""CaseHeaderId"",
                                  cd.casedetailid                               AS ""CaseDetailId"",
                                  cct.casetemplateid                            AS ""CaseTemplateId"",
                                  cct.casetemplatename                          AS ""CaseTemplateName"",
                                  ch.memberdetailid                              AS ""MemberDetailId"",
                                  ch.casetype::text                              AS ""CaseType"",
                                  COALESCE(ct.casetype_name, ch.casetype::text)  AS ""CaseTypeText"",
                                  concat_ws(' ', md.firstname, md.lastname)      AS ""MemberName"",
                                  md.memberid                                    AS ""MemberId"",
                                  su.username                                    AS ""CreatedByUserName"",
                                  ch.createdby                                   AS ""CreatedBy"",
                                  ch.createdon                                   AS ""CreatedOn"",
                                  cd.caselevelid                                 AS ""CaseLevelId"",
                                  (cd.jsondata::jsonb ->> 'Case_Overview_casePriority') AS ""CasePriority"",
                                  COALESCE(cp.casepriority_name,
                                           (cd.jsondata::jsonb ->> 'Case_Overview_casePriority')) AS ""CasePriorityText"",
                                  NULLIF(cd.jsondata::jsonb ->> 'Case_Overview_receivedDateTime','')::timestamptz
                                                                                AS ""ReceivedDateTime"",
                                  (cd.jsondata::jsonb ->> 'Case_Status_Details_caseStatus') AS ""CaseStatusId"",
                                  COALESCE(cs.casestatus_name,
                                           (cd.jsondata::jsonb ->> 'Case_Status_Details_caseStatus')) AS ""CaseStatusText"",
                                  COALESCE(cd.updatedon, cd.createdon)            AS ""LastDetailOn""
                                FROM caseheader ch
                                JOIN LATERAL (
                                  SELECT d.*
                                  FROM casedetail d
                                  WHERE d.caseheaderid = ch.caseheaderid
                                  ORDER BY d.caselevelid DESC NULLS LAST,
                                           COALESCE(d.updatedon, d.createdon) DESC NULLS LAST,
                                           d.casedetailid DESC
                                  LIMIT 1
                                ) cd ON TRUE
                                JOIN memberdetails md ON md.memberdetailsid = ch.memberdetailid
                                JOIN cfgcasetemplate cct on cct.casetemplateid = ch.casetype::int
                                JOIN securityuser  su ON su.userid = ch.createdby
                                LEFT JOIN casetype_lu    ct ON ct.id = ch.casetype::text
                                LEFT JOIN casestatus_lu  cs ON cs.id = (cd.jsondata::jsonb ->> 'Case_Status_Details_caseStatus')
                                LEFT JOIN casepriority_lu cp ON cp.id = (cd.jsondata::jsonb ->> 'Case_Overview_casePriority')
                                WHERE ch.createdby = @UserId
                                ORDER BY COALESCE(cd.updatedon, cd.createdon) DESC NULLS LAST;
                                ";

            await using var conn = new NpgsqlConnection(_connectionString);
            var rows = await conn.QueryAsync<AgCaseRow>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
            //var cmd = new CommandDefinition(sql, cancellationToken: ct);

            //var rows = await conn.QueryAsync<AgCaseRow>(cmd);
            return rows.AsList();
        }
    }


}