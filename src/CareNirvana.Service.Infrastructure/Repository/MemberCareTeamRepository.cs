using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
using Dapper;


namespace CareNirvana.Service.Infrastructure.Repository
{
    public class MemberCareTeamRepository:IMemberCareTeamRepository
    {
        private readonly string _connectionString;
        public MemberCareTeamRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        private NpgsqlConnection Open() => new NpgsqlConnection(_connectionString);

        // Aliases — adjust table/column names here if your schema differs
        private const string TblMcs = "public.membercarestaff mcs";
        private const string TblUser = "public.securityuserdetail sud";
        private const string TblMem = "public.memberdetails md";

        private const string SelectJoined = @"
                SELECT
                  mcs.membercarestaffid       AS MemberCareStaffId,
                  mcs.userid                  AS UserId,
                  mcs.memberdetailsid         AS MemberDetailsId,
                  COALESCE(mcs.activeflag, true) AS ActiveFlag,
                  mcs.startdate               AS StartDate,
                  mcs.enddate                 AS EndDate,
                  mcs.createdon               AS CreatedOn,
                  mcs.createdby               AS CreatedBy,
                  mcs.updatedon               AS UpdatedOn,
                  mcs.updatedby               AS UpdatedBy,
                  mcs.deletedon               AS DeletedOn,
                  mcs.deletedby               AS DeletedBy,
                  su.username                AS UserName,
                  sud.firstname               AS UserFirstName,
                  sud.lastname                AS UserLastName,
                  md.firstname                AS MemberFirstName,
                  md.lastname                 AS MemberLastName
                FROM public.membercarestaff mcs
                LEFT JOIN public.securityuser su ON su.userid = mcs.userid
                LEFT JOIN public.securityuserdetail sud ON sud.userdetailid = su.userid
                LEFT JOIN public.memberdetails md ON md.memberdetailsid = mcs.memberdetailsid
                ";

        public async Task<MemberCareStaffView?> GetAsync(int memberCareStaffId)
        {
            var sql = $"{SelectJoined} WHERE mcs.membercarestaffid = @id";
            using var conn = Open();
            return await conn.QueryFirstOrDefaultAsync<MemberCareStaffView>(sql, new { id = memberCareStaffId });
        }

        public async Task<PagedResult<MemberCareStaffView>> ListAsync(
            int? userId, int? memberDetailsId, bool includeInactive,
            int page, int pageSize, string? search = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 500);
            var offset = (page - 1) * pageSize;

            var where = new List<string>();
            var p = new DynamicParameters();

            if (!includeInactive) where.Add("COALESCE(mcs.activeflag, true) = true");
            if (userId.HasValue) { where.Add("mcs.userid = @userId"); p.Add("userId", userId); }
            if (memberDetailsId.HasValue) { where.Add("mcs.memberdetailsid = @memberDetailsId"); p.Add("memberDetailsId", memberDetailsId); }

            if (!string.IsNullOrWhiteSpace(search))
            {
                // search by username or member name
                where.Add(@"(
                    sud.username ILIKE @q OR
                    CONCAT(COALESCE(md.firstname,''), ' ', COALESCE(md.lastname,'')) ILIKE @q
                )");
                p.Add("q", $"%{search.Trim()}%");
            }

            var whereSql = where.Count > 0 ? $"WHERE {string.Join(" AND ", where)}" : string.Empty;

            var sqlTotal = $@"SELECT COUNT(*) FROM {TblMcs}
                    LEFT JOIN public.securityuserdetail sud ON sud.userdetailid = mcs.userid
                    LEFT JOIN public.memberdetails md ON md.memberdetailsid = mcs.memberdetailsid
                    {whereSql};";

            var sqlItems = $@"
                {SelectJoined}
                {whereSql}
                ORDER BY mcs.membercarestaffid DESC
                LIMIT @take OFFSET @skip;
                ";
            p.Add("take", pageSize);
            p.Add("skip", offset);

            using var conn = Open();
            var total = await conn.ExecuteScalarAsync<int>(sqlTotal, p);
            var items = (await conn.QueryAsync<MemberCareStaffView>(sqlItems, p)).ToArray();

            return new PagedResult<MemberCareStaffView> { Total = total, Items = items };
        }

        public async Task<int> CreateAsync(MemberCareStaffCreateRequest req)
        {
            var sql = @"
                INSERT INTO public.membercarestaff
                (userid, memberdetailsid, activeflag, startdate, enddate, createdon, createdby)
                VALUES (@UserId, @MemberDetailsId, @ActiveFlag, @StartDate, @EndDate, NOW(), @CreatedBy)
                RETURNING membercarestaffid;
                ";
            using var conn = Open();
            var id = await conn.ExecuteScalarAsync<int>(sql, req);
            return id;
        }

        public async Task<bool> UpdateAsync(int memberCareStaffId, MemberCareStaffUpdateRequest req)
        {
            // Build partial update
            var sets = new List<string> { "updatedon = NOW()", "updatedby = @UpdatedBy" };
            var p = new DynamicParameters(new { memberCareStaffId });

            if (req.UserId.HasValue) { sets.Add("userid = @UserId"); p.Add("UserId", req.UserId); }
            if (req.MemberDetailsId.HasValue) { sets.Add("memberdetailsid = @MemberDetailsId"); p.Add("MemberDetailsId", req.MemberDetailsId); }
            if (req.ActiveFlag.HasValue) { sets.Add("activeflag = @ActiveFlag"); p.Add("ActiveFlag", req.ActiveFlag); }
            if (req.StartDate.HasValue) { sets.Add("startdate = @StartDate"); p.Add("StartDate", req.StartDate); }
            if (req.EndDate.HasValue) { sets.Add("enddate = @EndDate"); p.Add("EndDate", req.EndDate); }
            p.Add("UpdatedBy", req.UpdatedBy);

            var sql = $@"
                UPDATE public.membercarestaff
                SET {string.Join(", ", sets)}
                WHERE membercarestaffid = @memberCareStaffId;";

            using var conn = Open();
            var rows = await conn.ExecuteAsync(sql, p);
            return rows > 0;
        }

        public async Task<bool> SoftDeleteAsync(int memberCareStaffId, int? deletedBy)
        {
            var sql = @"
                UPDATE public.membercarestaff
                SET activeflag = false, deletedon = NOW(), deletedby = @deletedBy
                WHERE membercarestaffid = @memberCareStaffId;";
            using var conn = Open();
            var rows = await conn.ExecuteAsync(sql, new { memberCareStaffId, deletedBy });
            return rows > 0;
        }
    }
}
