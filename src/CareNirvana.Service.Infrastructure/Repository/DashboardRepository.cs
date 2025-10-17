using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
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
                (SELECT COUNT(DISTINCT m.memberdetailsid)  FROM public.membercarestaff m   WHERE m.userid = @userId AND COALESCE(m.activeflag, true) = true) AS mymembercount,
                (SELECT COUNT(*) FROM public.authdetail a WHERE a.authassignedto = @userId) AS authcount,
                (SELECT COUNT(*) FROM public.authactivity aa WHERE aa.referto = @userId and aa.service_line_count=0) AS activitycount,
                (SELECT COUNT(*) FROM public.authactivity aa WHERE aa.referto = @userId and aa.service_line_count<>0 and md_review_status <> 'Approved') AS wqcount,
                (SELECT COUNT(*) FROM public.faxfiles ) AS faxcount
            ;";

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
                    // Remaining set to 0 for now
                    RequestCount = 0,
                    ComplaintCount = 0,
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
                                    mp.memberphonenumberid,
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
                                  on ma.memberdetailsid = md.memberdetailsid
                                left join memberphonenumber mp 
                                  on mp.memberdetailsid = md.memberdetailsid
                                left join vw_member_enrollment_hierarchy_json hie
                                  on hie.memberdetailsid = md.memberdetailsid and hie.enddate > current_date
                                left join lateral (
                                    select elem->>'code' as risklevel_code
                                    from cfgadmindata cad,
                                         jsonb_array_elements(cad.jsoncontent::jsonb->'risklevel') elem
                                    where (elem->>'id')::int = mr.risklevelid
                                      and cad.module = 'ADMIN'
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
                    MemberPhoneNumberId = reader.IsDBNull(reader.GetOrdinal("memberphonenumberid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberphonenumberid")),
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
                                    mp.memberphonenumberid,
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
                                  on ma.memberdetailsid = md.memberdetailsid
                                left join memberphonenumber mp 
                                  on mp.memberdetailsid = md.memberdetailsid
                                left join vw_member_enrollment_hierarchy_json hie
                                  on hie.memberdetailsid = md.memberdetailsid
                                left join lateral (
                                    select elem->>'code' as risklevel_code
                                    from cfgadmindata cad,
                                         jsonb_array_elements(cad.jsoncontent::jsonb->'risklevel') elem
                                    where (elem->>'id')::int = mr.risklevelid
                                      and cad.module = 'ADMIN'
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
                    MemberPhoneNumberId = reader.IsDBNull(reader.GetOrdinal("memberphonenumberid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberphonenumberid")),
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
                      ad.authnumber,
                      ad.authstatus,
                      rl.authstatusvalue,
                      at.templatename,
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
                      -- from Auth Details (header-level)
                      ah.treatmenttype_hdr  AS treatmenttype,
                      tt.treatmentTypeValue,
                      ah.authpriority_hdr   AS authpriority,
                      rp.requestPriorityValue,
                      concat(md.firstname, ' ', md.lastname) AS membername
                    FROM public.authdetail ad

                    -- normalize JSON root (array[0] vs object)
                    LEFT JOIN LATERAL (
                      SELECT CASE
                               WHEN jsonb_typeof(ad.data::jsonb) = 'array' THEN ad.data::jsonb -> 0
                               ELSE ad.data::jsonb
                             END AS root
                    ) j ON TRUE

                    -- header-level fields (Auth Details -> entries[0])
                    LEFT JOIN LATERAL (
                      SELECT
                        (j.root->'Auth Details'->'entries'->0->>'treatmentType')   AS treatmenttype_hdr,
                        (j.root->'Auth Details'->'entries'->0->>'requestPriority') AS authpriority_hdr
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
                      SELECT elem->>'requestPriority' AS requestPriorityValue
                      FROM cfgadmindata cad,
                           jsonb_array_elements(cad.jsoncontent::jsonb->'requestpriority') elem
                      WHERE (elem->>'id')::text = ah.authpriority_hdr
                        AND cad.module = 'UM'
                      LIMIT 1
                    ) rp ON TRUE

                    LEFT JOIN LATERAL (
                      SELECT elem->>'treatmentType' AS treatmentTypeValue
                      FROM cfgadmindata cad,
                           jsonb_array_elements(cad.jsoncontent::jsonb->'treatmenttype') elem
                      WHERE (elem->>'id')::text = ah.treatmenttype_hdr
                        AND cad.module = 'UM'
                      LIMIT 1
                    ) tt ON TRUE

                    LEFT JOIN authtemplate at ON at.id = ad.authtypeid
                    LEFT JOIN securityuser su ON su.userid = ad.createdby
                    LEFT JOIN memberdetails md on md.memberdetailsid = ad.memberdetailsid
                    WHERE ad.deletedby IS NULL and ad.authassignedto = @userId
                    ORDER BY ad.createdon DESC;";

            var results = new List<AuthDetailListItem>();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            while (await reader.ReadAsync())
            {
                var item = new AuthDetailListItem
                {
                    AuthNumber = reader["authnumber"]?.ToString() ?? "",
                    AuthStatus = reader.IsDBNull(reader.GetOrdinal("authstatus")) ? null : reader.GetInt32(reader.GetOrdinal("authstatus")),
                    AuthStatusValue = reader.IsDBNull(reader.GetOrdinal("authstatusvalue")) ? null : reader.GetString(reader.GetOrdinal("authstatusvalue")),
                    TemplateName = reader.IsDBNull(reader.GetOrdinal("templatename")) ? null : reader.GetString(reader.GetOrdinal("templatename")),
                    AuthClassValue = reader.IsDBNull(reader.GetOrdinal("authclassvalue")) ? null : reader.GetString(reader.GetOrdinal("authclassvalue")),

                    MemberDetailsId = reader.IsDBNull(reader.GetOrdinal("memberdetailsid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberdetailsid")),
                    MemberId = reader.IsDBNull(reader.GetOrdinal("memberid")) ? 0 : reader.GetInt32(reader.GetOrdinal("memberid")),
                    NextReviewDate = reader.IsDBNull(reader.GetOrdinal("nextreviewdate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("nextreviewdate")),
                    AuthDueDate = reader.IsDBNull(reader.GetOrdinal("authduedate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("authduedate")),

                    CreatedOn = reader.GetDateTime(reader.GetOrdinal("createdon")),
                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("createdby")) ? 0 : reader.GetInt32(reader.GetOrdinal("createdby")),
                    CreatedUser = reader.IsDBNull(reader.GetOrdinal("createduser")) ? null : reader.GetString(reader.GetOrdinal("createduser")),
                    UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updatedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("updatedon")),
                    UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updatedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("updatedby")),

                    TreatmentType = reader.IsDBNull(reader.GetOrdinal("treatmenttype")) ? null : reader.GetString(reader.GetOrdinal("treatmenttype")),
                    TreatmentTypeValue = reader.IsDBNull(reader.GetOrdinal("treatmentTypeValue")) ? null : reader.GetString(reader.GetOrdinal("treatmentTypeValue")),
                    AuthPriority = reader.IsDBNull(reader.GetOrdinal("authpriority")) ? null : reader.GetString(reader.GetOrdinal("authpriority")),
                    RequestPriorityValue = reader.IsDBNull(reader.GetOrdinal("requestPriorityValue")) ? null : reader.GetString(reader.GetOrdinal("requestPriorityValue")),
                    MemberName = reader.IsDBNull(reader.GetOrdinal("membername")) ? null : reader.GetString(reader.GetOrdinal("membername"))
                };

                results.Add(item);
            }

            return results;
        }

        public async Task<List<AuthActivityItem>> GetPendingAuthActivitiesAsync(int? userId = null)
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
                    'Pending' as status,
                    ad.authnumber
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
                order by aa.createdon desc;";

            var results = new List<AuthActivityItem>();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            if (userId.HasValue) cmd.Parameters.AddWithValue("@userId", userId.Value);
            else cmd.Parameters.AddWithValue("@userId", DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            while (await reader.ReadAsync())
            {
                var item = new AuthActivityItem
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
                    AuthNumber = reader.IsDBNull(reader.GetOrdinal("authnumber")) ? null : reader.GetString(reader.GetOrdinal("authnumber"))
                };

                results.Add(item);
            }

            return results;
        }

        public async Task<List<AuthActivityItem>> GetPendingWQAsync(int? userId = null)
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

            var results = new List<AuthActivityItem>();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            if (userId.HasValue) cmd.Parameters.AddWithValue("@userId", userId.Value);
            else cmd.Parameters.AddWithValue("@userId", DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            while (await reader.ReadAsync())
            {
                var item = new AuthActivityItem
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
            string mdNotes,
            int reviewedByUserId)
        {
            if (lineIds == null || !lineIds.Any())
                return 0;

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync();

            const string sql = @"
                UPDATE authactivityline
                SET status = @status,
                    mddecision = @mdDecision,
                    mdnotes = @mdNotes,
                    reviewedbyuserid = @reviewedByUserId,
                    reviewedon = NOW(),
                    updatedon = NOW()
                WHERE id = ANY(@ids);";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@status", status ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@mdDecision", mdDecision ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@mdNotes", mdNotes ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@reviewedByUserId", reviewedByUserId);
            var idArray = lineIds.ToArray();
            var p = cmd.Parameters.Add("@ids", NpgsqlDbType.Array | NpgsqlDbType.Integer);
            p.Value = idArray;

            //await tx.CommitAsync();

            var affected = await cmd.ExecuteNonQueryAsync();

            const string rollupSql = @"
            WITH affected AS (
                SELECT DISTINCT activityid
                FROM authactivityline
                WHERE id = ANY(@ids)
            ),
            agg AS (
                SELECT
                    l.activityid,
                    COUNT(*) AS total,
                    COUNT(*) FILTER (WHERE l.status = 'Approved') AS completed,
                    COUNT(*) FILTER (WHERE l.status <> 'Approved') AS not_approved,
                    COUNT(*) FILTER (WHERE l.mddecision = 'Approved' AND l.status = 'Approved') AS approved,
                    COUNT(*) FILTER (WHERE l.mddecision = 'Denied'   AND l.status = 'Denied')   AS denied_count
                FROM authactivityline l
                INNER JOIN affected a ON a.activityid = l.activityid
                GROUP BY l.activityid
            )
            UPDATE authactivity a
            SET service_line_count    = agg.total,
                md_review_status      = CASE
                                          WHEN agg.approved = agg.total THEN 'Approved'
                                          WHEN agg.approved > 0         THEN 'InProgress'
                                          ELSE 'Pending'
                                        END,
                md_aggregate_decision = CASE
                                          WHEN agg.approved = 0         THEN 'Pending'
                                          WHEN agg.approved = agg.total THEN 'Approved'
                                          WHEN agg.denied_count = agg.total THEN 'Denied'
                                          ELSE 'Mixed'
                                        END
            FROM agg
            WHERE a.authactivityid = agg.activityid;";

            await using var roll = new NpgsqlCommand(rollupSql, conn, tx);
            // reuse the same ids array
            var p2 = roll.Parameters.Add("@ids", NpgsqlDbType.Array | NpgsqlDbType.Integer);
            p2.Value = idArray;

            await roll.ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return affected;
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
                    createdon, createdby, updatedon, updatedby
                )
                VALUES
                (
                    @filename, @storedpath, @originalname, @contenttype, @sizebytes, @sha256hex,
                    @receivedat, @uploadedby, @uploadedat,
                    @pagecount, @memberid, @workbasket, @priority, @status, @processstatus,
                    @meta, @ocrtext, @ocrjsonpath, @filebytes,
                    @createdon, @createdby, @updatedon, @updatedby
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
                    storedpath    = @storedpath,
                    originalname  = @originalname,
                    contenttype   = @contenttype,
                    sizebytes     = @sizebytes,
                    sha256hex     = @sha256hex,
                    receivedat    = @receivedat,
                    uploadedby    = @uploadedby,
                    uploadedat    = @uploadedat,
                    pagecount     = @pagecount,
                    memberid      = @memberid,
                    workbasket    = @workbasket,
                    priority      = @priority,
                    status        = @status,
                    processstatus = @processstatus,
                    meta          = @meta,
                    ocrtext       = @ocrtext,
                    ocrjsonpath   = @ocrjsonpath,
                    updatedon     = @updatedon,
                    updatedby     = @updatedby
                WHERE faxid = @faxid;";

            using var cmd = new NpgsqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@faxid", fax.FaxId);
            cmd.Parameters.AddWithValue("@filename", fax.FileName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@storedpath", fax.Url ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@originalname", (object?)fax.OriginalName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@contenttype", (object?)fax.ContentType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sizebytes", (object?)fax.SizeBytes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sha256hex", (object?)fax.Sha256Hex ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@receivedat", fax.ReceivedAt);
            cmd.Parameters.AddWithValue("@uploadedby", (object?)fax.UploadedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@uploadedat", (object?)fax.UploadedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pagecount", fax.PageCount);
            cmd.Parameters.AddWithValue("@memberid", (object?)fax.MemberId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@workbasket", (object?)fax.WorkBasket ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@priority", fax.Priority);
            cmd.Parameters.AddWithValue("@status", fax.Status ?? "New");
            cmd.Parameters.AddWithValue("@processstatus", fax.ProcessStatus ?? "Pending");

            if (string.IsNullOrWhiteSpace(fax.MetaJson))
                cmd.Parameters.AddWithValue("@meta", NpgsqlDbType.Jsonb, DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@meta", NpgsqlDbType.Jsonb, fax.MetaJson);

            cmd.Parameters.AddWithValue("@ocrtext", (object?)fax.OcrText ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ocrjsonpath", (object?)fax.OcrJsonPath ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@updatedon", fax.UpdatedOn ?? DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@updatedby", (object?)fax.UpdatedBy ?? DBNull.Value);

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
                    f.workbasket,
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
                    COUNT(*) OVER() AS total_count
                FROM faxfiles f
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
                    UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updatedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("updatedby"))
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
                    createdon, createdby, updatedon, updatedby
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
                    UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updatedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("updatedby"))
                };
            }

            return null;
        }
    }

}