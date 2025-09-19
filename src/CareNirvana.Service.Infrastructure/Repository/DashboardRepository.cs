using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

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
                (SELECT COUNT(*) FROM public.authactivity      aa WHERE aa.referredto = @userId) AS activitycount
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
                    WQCount = 0
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
                    AuthCount = reader.GetInt32(reader.GetOrdinal("authcount"))
                };

                results.Add(o);
            }

            return results;
        }
    }
}