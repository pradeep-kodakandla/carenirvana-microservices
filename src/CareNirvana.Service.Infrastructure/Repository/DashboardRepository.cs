using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
using System.Net.NetworkInformation;

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
                (SELECT COUNT(*) FROM public.membercarestaff  m  WHERE m.userid = @userId AND COALESCE(m.activeflag, true) = true) AS mymembercount,
                (SELECT COUNT(*) FROM public.authdetail       a WHERE a.authassignedto = @userId) AS authcount,
                (SELECT COUNT(*) FROM public.authactivity      aa WHERE aa.referredto = @userId and aa.service_line_count=0) AS activitycount,
                (SELECT COUNT(*) FROM public.authactivity      aa WHERE aa.referredto = @userId and aa.service_line_count<>0) AS wqcount
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
                    FaxCount = 0,
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
                                        to_char(md.birthdate::date, 'MM-DD-YYYY') as birthdate,
                                        mr.riskscore,
                                        mr.risklevelid,
                                        rl.risklevel_code,
                                        null as lastcontact,
                                        null as nextcontact,
                                        ma.city,
                                        mp.memberphonenumberid,
                                        hie.level_map,
	                                    mc.startdate,
	                                    mc.enddate,
                                        coalesce(ac.authcount, 0) as authcount
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
                                    left join (
                                        select ad.memberid, count(*) as authcount
                                        from authdetail ad
                                        group by ad.memberid
                                    ) ac on ac.memberid = md.memberid
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
                    DOB = reader.IsDBNull(reader.GetOrdinal("birthdate")) ? null : reader.GetString(reader.GetOrdinal("birthdate")),

                    RiskScore = reader.IsDBNull(reader.GetOrdinal("riskscore")) ? (decimal?)null : reader.GetFieldValue<decimal>(reader.GetOrdinal("riskscore")),
                    RiskLevelId = reader.IsDBNull(reader.GetOrdinal("risklevelid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("risklevelid")),
                    RiskLevelCode = reader.IsDBNull(reader.GetOrdinal("risklevel_code")) ? null : reader.GetString(reader.GetOrdinal("risklevel_code")),

                    LastContact = reader.IsDBNull(reader.GetOrdinal("lastcontact")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("lastcontact")),
                    NextContact = reader.IsDBNull(reader.GetOrdinal("nextcontact")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("nextcontact")),

                    City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString(reader.GetOrdinal("city")),
                    MemberPhoneNumberId = reader.IsDBNull(reader.GetOrdinal("memberphonenumberid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberphonenumberid")),
                    LevelMap = reader.IsDBNull(reader.GetOrdinal("level_map")) ? null : reader.GetString(reader.GetOrdinal("level_map")),
                    StartDate = reader.IsDBNull(reader.GetOrdinal("startdate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("startdate")),
                    EndDate = reader.IsDBNull(reader.GetOrdinal("enddate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("enddate")),
                    AuthCount = reader.GetInt32(reader.GetOrdinal("authcount"))

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
                      ad.memberid,
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
                    LEFT JOIN memberdetails md on md.memberid = ad.memberid
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
                    aa.createdon,
                    aa.activitytypeid,
                    at.activitytype,
                    aa.referredto,
                    su.username,
                    aa.followupdatetime,
                    aa.duedate,
                    aa.statusid,
                    'Pending' as status
                from authactivity aa
                join authdetail ad on ad.authdetailid = aa.authdetailid
                join memberdetails md on md.memberid = ad.memberid
                join securityuser su on su.userid = aa.referredto
                left join lateral (
                    select elem->>'activityType' as activitytype
                    from cfgadmindata cad,
                         jsonb_array_elements(cad.jsoncontent::jsonb->'activitytype') elem
                    where (elem->>'id')::int = aa.activitytypeid
                      and cad.module = 'UM'
                    limit 1
                ) at on true
                where aa.service_line_count = 0
                  and (@userId is null or aa.referredto = @userId)
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

                    CreatedOn = reader.IsDBNull(reader.GetOrdinal("createdon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("createdon")),

                    ActivityTypeId = reader.IsDBNull(reader.GetOrdinal("activitytypeid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("activitytypeid")),
                    ActivityType = reader.IsDBNull(reader.GetOrdinal("activitytype")) ? null : reader.GetString(reader.GetOrdinal("activitytype")),

                    ReferredTo = reader.IsDBNull(reader.GetOrdinal("referredto")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("referredto")),
                    UserName = reader.IsDBNull(reader.GetOrdinal("username")) ? null : reader.GetString(reader.GetOrdinal("username")),

                    FollowUpDateTime = reader.IsDBNull(reader.GetOrdinal("followupdatetime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("followupdatetime")),
                    DueDate = reader.IsDBNull(reader.GetOrdinal("duedate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("duedate")),

                    StatusId = reader.IsDBNull(reader.GetOrdinal("statusid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("statusid")),
                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? null : reader.GetString(reader.GetOrdinal("status"))
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
                    aa.createdon,
                    aa.activitytypeid,
                    at.activitytype,
                    aa.referredto,
                    su.username,
                    aa.followupdatetime,
                    aa.duedate,
                    aa.statusid,
                    'Pending' as status,
                    ad.authnumber,
                    aa.comment,
                    aa.authactivityid
                from authactivity aa
                join authdetail ad on ad.authdetailid = aa.authdetailid
                join memberdetails md on md.memberid = ad.memberid
                join securityuser su on su.userid = aa.referredto
                left join lateral (
                    select elem->>'activityType' as activitytype
                    from cfgadmindata cad,
                         jsonb_array_elements(cad.jsoncontent::jsonb->'activitytype') elem
                    where (elem->>'id')::int = aa.activitytypeid
                      and cad.module = 'UM'
                    limit 1
                ) at on true
                where aa.service_line_count <> 0
                  and (@userId is null or aa.referredto = @userId)
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

                    CreatedOn = reader.IsDBNull(reader.GetOrdinal("createdon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("createdon")),

                    ActivityTypeId = reader.IsDBNull(reader.GetOrdinal("activitytypeid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("activitytypeid")),
                    ActivityType = reader.IsDBNull(reader.GetOrdinal("activitytype")) ? null : reader.GetString(reader.GetOrdinal("activitytype")),

                    ReferredTo = reader.IsDBNull(reader.GetOrdinal("referredto")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("referredto")),
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
                reviewedbyuserid, reviewedon, updatedon, version
                from authactivityline
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
            cmd.Parameters.AddWithValue("@ids", lineIds.ToArray());

            return await cmd.ExecuteNonQueryAsync();
        }

    }

}