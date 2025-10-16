using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using static CareNirvana.Service.Domain.Model.MemberJourney;
using System.Linq;

namespace CareNirvana.Service.Infrastructure.Repository
{
    public class MemberJourneyRepository : IMemberJourney
    {
        private readonly string _connString;

        public MemberJourneyRepository(IConfiguration configuration)
        {
            _connString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<(Domain.Model.MemberJourney.PagedResult<MemberJourneyEvent> Page, MemberJourneySummary Summary)>
            GetMemberJourneyAsync(MemberJourneyRequest req)
        {
            var fromUtc = req.FromUtc ?? DateTime.UtcNow.AddDays(-30);
            var toUtc = req.ToUtc ?? DateTime.UtcNow;
            var offset = Math.Max(0, (req.Page - 1) * req.PageSize);

            // Category filter—build a WHERE IN (…) against our union rows
            string? categoryFilterClause = null;
            var categoryParams = new DynamicParameters();
            if (req.Categories != null && req.Categories.Any())
            {
                var cats = req.Categories.ToArray();
                categoryFilterClause = " AND evt.category = ANY(@cats) ";
                categoryParams.Add("@cats", cats.Select(c => (int)c).ToArray());
            }

            // Search filter—applies to title/subtitle (ILIKE for Postgres)
            var searchClause = string.IsNullOrWhiteSpace(req.Search)
                ? string.Empty
                : " AND (evt.title ILIKE @search OR evt.subtitle ILIKE @search) ";

            var sb = new StringBuilder(@"
                WITH evt AS (
                    -- AUTH DETAIL
                    SELECT
                        'authdetail:  ' || ad.authdetailid::text     AS event_id,
                        ad.memberdetailsid                         AS memberdetailsid,
                        1                                          AS category, 
                        'Authorization:  ' || COALESCE(ad.authnumber::text, 'Authorization') AS title, 
                        COALESCE(ad.authstatus::text, '')              AS subtitle,            
                        COALESCE(ad.createdon, ad.createdon)::timestamptz AS eventutc,
                        'auth'                                     AS icon,
                        ad.authdetailid::text                      AS sourceid,
                        'authdetail'                               AS sourcetable,
                        NULL::text                                 AS actionurl,
                        NULL::text                                 AS extrajson
                    FROM authdetail ad
                    WHERE ad.memberdetailsid = @memberdetailsid

                    UNION ALL

                    -- AUTH ACTIVITY
                     SELECT
                         'authactivity:  ' || aa.authactivityid::text AS event_id,
                         ad.memberdetailsid                          AS memberdetailsid,
                         2                                           AS category, -- AuthActivity
                         'Authorization Activity:  ' ||COALESCE(aa.activitytypeid::text, 'Activity')       AS title,    
                         COALESCE(aa.comment, '')                      AS subtitle,
                         COALESCE(aa.createdon, aa.followupdatetime)::timestamptz AS eventutc,
                         'activity'                                  AS icon,
                         aa.authactivityid::text                     AS sourceid,
                         'authactivity'                              AS sourcetable,
                         NULL::text                                  AS actionurl,
                         NULL::text                                  AS extrajson
                     FROM authactivity aa
                     JOIN authdetail ad on ad.authdetailid = aa.authdetailid
                     WHERE ad.memberdetailsid = @memberdetailsid

                    UNION ALL

                    -- ENROLLMENT
                       SELECT
                           'memberenrollment:  ' || me.memberenrollmentid::text AS event_id,
                           me.memberdetailsid                                  AS memberdetailsid,
                           3                                                   AS category, -- Enrollment
                           'Enrollment:  ' || COALESCE(me.status::text, 'Enrollment')             AS title,    -- bool/enum -> text if needed
                           COALESCE(hie.hierarchy_path, '')                      AS subtitle, -- if you have it
                           COALESCE(me.startdate, me.createdon)::timestamptz   AS eventutc,
                           'enrollment'                                        AS icon,
                           me.memberenrollmentid::text                         AS sourceid,
                           'memberenrollment'                                  AS sourcetable,
                           NULL::text                                          AS actionurl,
                           NULL::text                                          AS extrajson
                       FROM memberenrollment me
				       join vw_member_enrollment_hierarchy_json hie on hie.memberenrollmentid= me.memberenrollmentid
                    WHERE me.memberdetailsid = @memberdetailsid

                    UNION ALL

                    -- CARE STAFF
                    SELECT
                        'membercarestaff:  ' || mcs.membercarestaffid::text   AS event_id,
                        mcs.memberdetailsid                                 AS memberdetailsid,
                        4                                                   AS category, -- CareStaff
                        'Care Staff Assigned:  ' ||COALESCE(su.username, 'Care Staff Assigned')       AS title,
                        CASE 
                            WHEN mcs.activeflag = true THEN 'Active' ELSE 'Inactive' 
                        END                                                 AS subtitle,
                        COALESCE(mcs.startdate, mcs.createdon)::timestamptz AS eventutc,
                        'carestaff'                                         AS icon,
                        mcs.membercarestaffid::text                         AS sourceid,
                        'membercarestaff'                                   AS sourcetable,
                        NULL::text                                          AS actionurl,
                        NULL::text                                          AS extrajson
                    FROM membercarestaff mcs
					join securityuser su on su.userid = mcs.userid
                    WHERE mcs.memberdetailsid = @memberdetailsid

                    UNION ALL

                    -- CAREGIVER
                    SELECT
                        'membercaregiver:  ' || mcg.membercaregiverid::text   AS event_id,
                        mcg.memberdetailsid                                 AS memberdetailsid,
                        5                                                   AS category, 
                        'Caregiver Linked:  ' ||COALESCE(mcg.caregiverfirstname || ' ' || mcg.caregiverlastname, 'Caregiver Linked') AS title,
                        COALESCE('Primary', '')                      AS subtitle,
                        COALESCE(mcg.createdon)::timestamptz AS eventutc,
                        'caregiver'                                         AS icon,
                        mcg.membercaregiverid::text                         AS sourceid,
                        'membercaregiver'                                   AS sourcetable,
                        NULL::text                                          AS actionurl,
                        NULL::text                                          AS extrajson
                    FROM membercaregiver mcg
                    WHERE mcg.memberdetailsid = @memberdetailsid

                    UNION ALL

                    -- PROGRAM
                    SELECT
                        'memberprogram:  ' || mp.memberprogramid::text        AS event_id,
                        mp.memberdetailsid                                  AS memberdetailsid,
                        6                                                   AS category, -- Program
                        'Program Assigned:  ' ||COALESCE(mp.programid::text, 'Program')                     AS title,
                        NULL::text                                          AS subtitle,
                        COALESCE(mp.startdate, mp.createdon)::timestamptz   AS eventutc,
                        'program'                                           AS icon,
                        mp.memberprogramid::text                            AS sourceid,
                        'memberprogram'                                     AS sourcetable,
                        NULL::text                                          AS actionurl,
                        NULL::text                                          AS extrajson
                    FROM memberprogram mp
                    WHERE mp.memberdetailsid = @memberdetailsid

                    UNION ALL

                    -- NOTE
                    SELECT
                        'membernote:  ' || mn.membernoteid::text              AS event_id,
                        mn.memberdetailsid                                  AS memberdetailsid,
                        7                                                   AS category, -- Note
                        'Note:  ' ||COALESCE(mn.notetypeid::text, 'Note')                      AS title,
                        LEFT(COALESCE(mn.membernotes, ''), 200)                   AS subtitle,
                        COALESCE(mn.createdon)::timestamptz AS eventutc,
                        'note'                                              AS icon,
                        mn.membernoteid::text                               AS sourceid,
                        'membernote'                                        AS sourcetable,
                        NULL::text                                          AS actionurl,
                        NULL::text                                          AS extrajson
                    FROM membernote mn
                    WHERE mn.memberdetailsid = @memberdetailsid
                    UNION ALL

                        -- MEMBER RISK (Risk score/level updates)
                    SELECT
                        'memberrisk:  ' || mr.memberriskid::text                 AS event_id,
                        mr.memberdetailsid                                     AS memberdetailsid,
                        8                                                      AS category, -- Risk
                        'Risk Updated:  ' ||
                        CASE mr.risklevelid
                            WHEN 1 THEN 'Low'
                            WHEN 2 THEN 'Medium'
                            WHEN 3 THEN 'High'
                            ELSE 'No Risk'
                        END
                        || COALESCE(' (' || mr.riskscore::text || ')', '')     AS title,
                        NULL::text                                             AS subtitle,
                        COALESCE(mr.createdon, mr.riskstartdate)::timestamptz  AS eventutc,
                        'risk'                                                 AS icon,
                        mr.memberriskid::text                                  AS sourceid,
                        'memberrisk'                                           AS sourcetable,
                        NULL::text                                             AS actionurl,
                        NULL::text                                             AS extrajson
                    FROM memberrisk mr
                    WHERE mr.memberdetailsid = @memberdetailsid

                    UNION ALL

                    -- MEMBER ALERT (alerts raised on the member)
                    SELECT
                        'memberalert:  ' || ma.memberalertid::text               AS event_id,
                        ma.memberdetailsid                                     AS memberdetailsid,
                        9                                                      AS category, -- Alert
                        -- Title example: ""Alert: High Risk OB""
                        'Alert: ' || COALESCE(ma.alertid::text, 'General')         AS title,
                        LEFT(COALESCE('Alert', ''), 200)               AS subtitle,
                        COALESCE(ma.createdon, ma.alertdate)::timestamptz      AS eventutc,
                        'alert'                                                AS icon,
                        ma.memberalertid::text                                 AS sourceid,
                        'memberalert'                                          AS sourcetable,
                        NULL::text                                             AS actionurl,
                        NULL::text                                             AS extrajson
                    FROM memberalert ma
                    WHERE ma.memberdetailsid = @memberdetailsid
                )

                SELECT * FROM evt
                WHERE evt.eventutc BETWEEN @fromUtc AND @toUtc
                ");

            // apply category filter
            if (!string.IsNullOrEmpty(categoryFilterClause))
                sb.AppendLine(categoryFilterClause);

            // apply search filter
            sb.AppendLine(searchClause);

            // total count
            var countSql = $@"
                            SELECT COUNT(*) FROM ({sb}) AS x;
                            ";

            // paged rows
            var pageSql = $@"
                                {sb}
                                ORDER BY evt.eventutc DESC
                                LIMIT @limit OFFSET @offset;
                                ";

            // summary counts by category (for the footer chips)
            var summarySql = $@"
                        SELECT 
                            COUNT(*) AS total,
                            COUNT(*) FILTER (WHERE category=1) AS authcount,
                            COUNT(*) FILTER (WHERE category=2) AS authactivitycount,
                            COUNT(*) FILTER (WHERE category=3) AS enrollmentcount,
                            COUNT(*) FILTER (WHERE category=4) AS carestaffcount,
                            COUNT(*) FILTER (WHERE category=5) AS caregivercount,
                            COUNT(*) FILTER (WHERE category=6) AS programcount,
                            COUNT(*) FILTER (WHERE category=7) AS notecount,
                            COUNT(*) FILTER (WHERE category=8) AS riskcount,
                            COUNT(*) FILTER (WHERE category=9) AS alertcount
                        FROM ({sb}) AS s;
                        ";

            using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync();

            var baseParams = new DynamicParameters();
            baseParams.Add("@memberdetailsid", req.MemberDetailsId);
            baseParams.Add("@fromUtc", fromUtc);
            baseParams.Add("@toUtc", toUtc);
            baseParams.Add("@limit", req.PageSize);
            baseParams.Add("@offset", offset);
            baseParams.Add("@search", string.IsNullOrWhiteSpace(req.Search) ? null : $"%{req.Search.Trim()}%");

            // add category array param if present
            foreach (var p in categoryParams.ParameterNames)
                baseParams.Add(p, categoryParams.Get<dynamic>(p));

            // run in one round-trip
            var total = await conn.ExecuteScalarAsync<int>(countSql, baseParams);
            var items = (await conn.QueryAsync<MemberJourneyEvent>(pageSql, baseParams)).ToList();

            var sum = await conn.QuerySingleAsync<MemberJourneySummary>(summarySql, baseParams);

            var paged = new Domain.Model.MemberJourney.PagedResult<MemberJourneyEvent>
            {
                Items = items,
                Page = req.Page,
                PageSize = req.PageSize,
                Total = total
            };

            return (paged, sum);
        }

    }
}
