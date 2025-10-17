using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dapper;


namespace CareNirvana.Service.Infrastructure.Repository
{
    public class RecentlyAccessedRepository : IRecentlyAccessed
    {
        private readonly string _connectionString;

        public RecentlyAccessedRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public async Task<int> InsertAsync(RecentlyAccessed item)
        {
            const string sql = @"
            INSERT INTO public.recentlyaccessed
                (userid, featureid, featuregroupid, accesseddatetime, action,
                 memberdetailsid, authdetailid, complaintdetailid)
            VALUES
                (@UserId, @FeatureId, @FeatureGroupId,
                 COALESCE(@AccessedDateTime, CURRENT_TIMESTAMP),
                 @Action, @MemberDetailsId, @AuthDetailId, @ComplaintDetailId)
            RETURNING recentlyaccessedid;";


            using var conn = new NpgsqlConnection(_connectionString);

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                item.UserId,
                item.FeatureId,
                item.FeatureGroupId,
                item.AccessedDateTime,
                item.Action,
                item.MemberDetailsId,
                item.AuthDetailId,
                item.ComplaintDetailId
            });
        }
        public async Task<IEnumerable<RecentlyAccessedView>> GetByUserAsync(
        int userId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int limit = 100,
        int offset = 0)
        {
            // Joins with cfgfeature and cfgfeaturegroup as requested
            const string sql = @"
            SELECT
                ra.recentlyaccessedid        AS RecentlyAccessedId,
                ra.userid                    AS UserId,
                ra.featureid                 AS FeatureId,
                cf.featurename               AS FeatureName,
                ra.featuregroupid            AS FeatureGroupId,
                cfg.featuregroupname         AS FeatureGroupName,
                ra.accesseddatetime          AS AccessedDateTime,
                ra.action                    AS Action,
                ra.memberdetailsid           AS MemberDetailsId,
                ra.authdetailid              AS AuthDetailId,
                ra.complaintdetailid         AS ComplaintDetailId,
				md.memberid					 AS MemberId,
				ad.authnumber				 AS AuthNumber,
                concat(md.firstname, ' ', md.lastname) AS membername
				
            FROM public.recentlyaccessed ra
            LEFT JOIN public.cfgfeature cf
                   ON cf.featureid = ra.featureid
            LEFT JOIN public.cfgfeaturegroup cfg
                   ON cfg.featuregroupid = ra.featuregroupid
			LEFT JOIN memberdetails md
					ON md.memberdetailsid = ra.memberdetailsid
			LEFT JOIN authdetail ad 
					on ad.authdetailid = ra.authdetailid
            WHERE ra.userid = @UserId
              AND (@FromUtc IS NULL OR ra.accesseddatetime >= @FromUtc)
              AND (@ToUtc   IS NULL OR ra.accesseddatetime <  @ToUtc)
            ORDER BY ra.accesseddatetime DESC
            LIMIT @Limit OFFSET @Offset;";

            using var conn = new NpgsqlConnection(_connectionString);
            return await conn.QueryAsync<RecentlyAccessedView>(sql, new
            {
                UserId = userId,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Limit = limit,
                Offset = offset
            });
        }

        public async Task<Last24hCounts> GetLast24hCountsAsync(int userId)
        {
            const string sql = @"
                WITH ra24h AS (
                  SELECT *
                  FROM public.recentlyaccessed
                  WHERE userid = @UserId
                    AND accesseddatetime >= (CURRENT_TIMESTAMP - INTERVAL '24 hours')
                )
                SELECT
                    COUNT(DISTINCT memberdetailsid)     AS MemberAccessCount,
                    COUNT(DISTINCT authdetailid)        AS AuthorizationAccessCount,
                    COUNT(DISTINCT complaintdetailid)   AS ComplaintAccessCount
                FROM ra24h;";

            using var conn = new NpgsqlConnection(_connectionString);
            return await conn.QuerySingleAsync<Last24hCounts>(sql, new { UserId = userId });
        }
    }
}
