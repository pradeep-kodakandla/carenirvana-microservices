using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
using Dapper;


namespace CareNirvana.Service.Infrastructure.Repository
{
    public class MemberAlertsRepository : IMemberAlertRepository
    {
        private readonly string _connectionString;
        public MemberAlertsRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public async Task<MemberAlertPagedResult> GetAlertsAsync(int[]? memberDetailsIds = null, int? alertId = null,
        bool activeOnly = true, int page = 1, int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 50;

            const string sql = @"
                    WITH admin_json AS (
                      SELECT jsoncontent::jsonb AS j
                      FROM cfgadmindata
                      WHERE module = 'ADMIN'
                      ORDER BY updatedon DESC NULLS LAST, createdon DESC NULLS LAST
                      LIMIT 1
                    ),
                    alert_source AS (
                      SELECT (e->>'id')::int AS id,
                             e->>'code' AS code,
                             e->>'alertSourceName' AS name
                      FROM admin_json aj
                      CROSS JOIN LATERAL jsonb_array_elements(aj.j->'alertsource') AS e
                      WHERE COALESCE((e->>'activeFlag')::boolean, true)
                    ),
                    alert_type AS (
                      SELECT (e->>'id')::int AS id,
                             e->>'code' AS code,
                             e->>'alertType' AS name
                      FROM admin_json aj
                      CROSS JOIN LATERAL jsonb_array_elements(aj.j->'alerttype') AS e
                      WHERE COALESCE((e->>'activeFlag')::boolean, true)
                    ),
                    alert_status AS (
                      SELECT (e->>'id')::int AS id,
                             e->>'code' AS code,
                             e->>'alertStatus' AS name
                      FROM admin_json aj
                      CROSS JOIN LATERAL jsonb_array_elements(aj.j->'alertstatus') AS e
                      WHERE COALESCE((e->>'activeFlag')::boolean, true)
                    )
                    SELECT
                      COUNT(*) OVER() AS totalcount,

                      ma.memberalertid              AS MemberAlertId,
                      ma.memberdetailsid            AS MemberDetailsId,
                      md.firstname                  AS MemberFirstName,
                      md.lastname                   AS MemberLastName,
                      ma.alertid                    AS AlertId,
                      ca.alertname                  AS CfgAlertName,

                      ma.altersourceid              AS AlterSourceId,
                      src.name                      AS AlertSourceName,
                      src.code                      AS AlertSourceCode,

                      ma.alerttypeid                AS AlertTypeId,
                      typ.name                      AS AlertTypeName,
                      typ.code                      AS AlertTypeCode,

                      ma.alertstatusid              AS AlertStatusId,
                      sts.name                      AS AlertStatusName,
                      sts.code                      AS AlertStatusCode,

                      ma.alertdate                  AS AlertDate,
                      ma.enddate                    AS EndDate,
                      ma.dismisseddate              AS DismissedDate,
                      ma.acknowledgeddate           AS AcknowledgedDate,
                      ma.activeflag                 AS ActiveFlag,
                      ma.createdon                  AS CreatedOn,
                      ma.createdby                  AS CreatedBy,
                      ma.updatedon                  AS UpdatedOn,
                      ma.updatedby                  AS UpdatedBy,
                      ma.deletedon                  AS DeletedOn,
                      ma.deletedby                  AS DeletedBy
                    FROM memberalert ma
                    LEFT JOIN cfgalert     ca ON ca.alertid        = ma.alertid
                    LEFT JOIN memberdetails md ON md.memberdetailsid = ma.memberdetailsid
                    LEFT JOIN alert_source  src ON src.id          = ma.altersourceid
                    LEFT JOIN alert_type    typ ON typ.id          = ma.alerttypeid
                    LEFT JOIN alert_status  sts ON sts.id          = ma.alertstatusid
                    WHERE
                      (@activeOnly = FALSE OR COALESCE(ma.activeflag, TRUE) = TRUE)
                      AND (
                            @memberDetailsIds IS NULL
                            OR ma.memberdetailsid = ANY(@memberDetailsIds)
                          )
                      AND (
                            @alertId IS NULL
                            OR ma.alertid = @alertId
                          )
                    ORDER BY ma.memberalertid
                    LIMIT @limit OFFSET @offset;
                    ";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var p = new
            {
                activeOnly,
                memberDetailsIds,
                alertId,
                limit = pageSize,
                offset = (page - 1) * pageSize
            };

            var rows = (await conn.QueryAsync<MemberAlertView>(sql, p)).ToList();

            long total = rows.FirstOrDefault()?.TotalCount ?? 0;
            // strip per-row TotalCount if you’d like (kept on each item is fine)
            return new MemberAlertPagedResult
            {
                Total = total,
                Items = rows
            };
        }

        public async Task<int?> UpdateAlertStatusAsync(int memberAlertId, int? alertStatusId = null,
            DateTime? dismissedDate = null, DateTime? acknowledgedDate = null, int updatedBy = 0)
        {
            const string sql = @"
                UPDATE memberalert
                SET
                    alertstatusid = COALESCE(@alertStatusId, alertstatusid),
                    dismisseddate = @dismissedDate,
                    acknowledgeddate = @acknowledgedDate,
                    updatedby = @updatedBy,
                    updatedon = NOW()
                WHERE memberalertid = @memberAlertId
                  AND COALESCE(activeflag, TRUE) = TRUE
                RETURNING memberalertid;";

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            return await conn.ExecuteScalarAsync<int?>(sql, new
            {
                memberAlertId,
                alertStatusId,
                dismissedDate,
                acknowledgedDate,
                updatedBy
            });
        }
    }
}
