using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace CareNirvana.Service.Infrastructure.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly string _connStr;

        public AuthRepository(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection CreateConn() => new NpgsqlConnection(_connStr);

        public async Task<AuthDetailRow?> GetAuthByNumberAsync(string authNumber, bool includeDeleted = false)
        {
            const string sql = @"
                with auth_status as (
                  select
                      (s->>'id')::int as AuthStatusId,
                      s->>'authStatus' as AuthStatusText,
                      coalesce((s->>'activeFlag')::boolean, false) as ActiveFlag
                  from cfgadmindata c
                  cross join lateral jsonb_array_elements(c.jsoncontent->'authstatus') s
                  where c.module = 'UM'
                )
                select
                    a.authdetailid as AuthDetailId,
                    a.authnumber as AuthNumber,
                    a.authtypeid as AuthTypeId,
                    at.authtemplatename as AuthTemplateName,
                    a.memberdetailsid as MemberDetailsId,
                    a.authduedate as AuthDueDate,
                    a.nextreviewdate as NextReviewDate,
                    a.treatementtype as TreatementType,
                    a.data::text as DataJson,
                    a.createdon as CreatedOn,
                    a.createdby as CreatedBy,
                    a.updatedon as UpdatedOn,
                    a.updatedby as UpdatedBy,
                    a.deletedon as DeletedOn,
                    a.deletedby as DeletedBy,
                    a.authclassid as AuthClassId,
                    a.authassignedto as AuthAssignedTo,
                    a.authstatus as AuthStatus,
                    st.AuthStatusText as AuthStatusText,
                    su.username as CreatedByUserName,
                    md.memberid as MemberId,
                    (coalesce(md.firstname,'') ||
                      case when md.lastname is null or md.lastname = '' then '' else ' ' || md.lastname end
                    ) as MemberName
                from authdetail a
                left join cfgauthtemplate at on at.authtemplateid = a.authtypeid
                left join securityuser su on su.userid = a.createdby
                left join memberdetails md on md.memberdetailsid = a.memberdetailsid
                left join auth_status st
                       on st.AuthStatusId = a.authstatus
                      and st.ActiveFlag = true
                where a.authnumber = @authNumber
                  and (@includeDeleted = true or a.deletedon is null)
                order by a.createdon desc;";

            await using var conn = CreateConn();
            return await conn.QueryFirstOrDefaultAsync<AuthDetailRow>(sql, new { authNumber, includeDeleted });
        }

        public async Task<AuthDetailRow?> GetAuthByIdAsync(long authDetailId, bool includeDeleted = false)
        {
            const string sql = @"
                with auth_status as (
                  select
                      (s->>'id')::int as AuthStatusId,
                      s->>'authStatus' as AuthStatusText,
                      coalesce((s->>'activeFlag')::boolean, false) as ActiveFlag
                  from cfgadmindata c
                  cross join lateral jsonb_array_elements(c.jsoncontent->'authstatus') s
                  where c.module = 'UM'
                )
                select
                    a.authdetailid as AuthDetailId,
                    a.authnumber as AuthNumber,
                    a.authtypeid as AuthTypeId,
                    at.authtemplatename as AuthTemplateName,
                    a.memberdetailsid as MemberDetailsId,
                    a.authduedate as AuthDueDate,
                    a.nextreviewdate as NextReviewDate,
                    a.treatementtype as TreatementType,
                    a.data::text as DataJson,
                    a.createdon as CreatedOn,
                    a.createdby as CreatedBy,
                    a.updatedon as UpdatedOn,
                    a.updatedby as UpdatedBy,
                    a.deletedon as DeletedOn,
                    a.deletedby as DeletedBy,
                    a.authclassid as AuthClassId,
                    a.authassignedto as AuthAssignedTo,
                    a.authstatus as AuthStatus,
                    st.AuthStatusText as AuthStatusText,
                    su.username as CreatedByUserName,
                    md.memberid as MemberId,
                    (coalesce(md.firstname,'') ||
                      case when md.lastname is null or md.lastname = '' then '' else ' ' || md.lastname end
                    ) as MemberName
                from authdetail a
                left join cfgauthtemplate at on at.authtemplateid = a.authtypeid
                left join securityuser su on su.userid = a.createdby
                left join memberdetails md on md.memberdetailsid = a.memberdetailsid
                left join auth_status st
                       on st.AuthStatusId = a.authstatus
                      and st.ActiveFlag = true
                where a.authdetailid = @authDetailId
                  and (@includeDeleted = true or a.deletedon is null)
                order by a.createdon desc;";


            await using var conn = CreateConn();
            return await conn.QueryFirstOrDefaultAsync<AuthDetailRow>(sql, new { authDetailId, includeDeleted });
        }

        public async Task<List<AuthDetailRow>> GetAuthsByMemberAsync(int memberDetailsId, bool includeDeleted = false)
        {
            const string sql = @"
                with auth_status as (
                  select
                      (s->>'id')::int as AuthStatusId,
                      s->>'authStatus' as AuthStatusText,
                      coalesce((s->>'activeFlag')::boolean, false) as ActiveFlag
                  from cfgadmindata c
                  cross join lateral jsonb_array_elements(c.jsoncontent->'authstatus') s
                  where c.module = 'UM'
                )
                select
                    a.authdetailid as AuthDetailId,
                    a.authnumber as AuthNumber,
                    a.authtypeid as AuthTypeId,
                    at.authtemplatename as AuthTemplateName,
                    a.memberdetailsid as MemberDetailsId,
                    a.authduedate as AuthDueDate,
                    a.nextreviewdate as NextReviewDate,
                    a.treatementtype as TreatementType,
                    a.data::text as DataJson,
                    a.createdon as CreatedOn,
                    a.createdby as CreatedBy,
                    a.updatedon as UpdatedOn,
                    a.updatedby as UpdatedBy,
                    a.deletedon as DeletedOn,
                    a.deletedby as DeletedBy,
                    a.authclassid as AuthClassId,
                    a.authassignedto as AuthAssignedTo,
                    a.authstatus as AuthStatus,
                    st.AuthStatusText as AuthStatusText,
                    su.username as CreatedByUserName,
                    md.memberid as MemberId,
                    (coalesce(md.firstname,'') ||
                      case when md.lastname is null or md.lastname = '' then '' else ' ' || md.lastname end
                    ) as MemberName
                from authdetail a
                left join cfgauthtemplate at on at.authtemplateid = a.authtypeid
                left join securityuser su on su.userid = a.createdby
                left join memberdetails md on md.memberdetailsid = a.memberdetailsid
                left join auth_status st
                       on st.AuthStatusId = a.authstatus
                      and st.ActiveFlag = true
                where a.memberdetailsid = @memberDetailsId
                  and (@includeDeleted = true or a.deletedon is null)
                order by a.createdon desc;";


            await using var conn = CreateConn();
            var rows = await conn.QueryAsync<AuthDetailRow>(sql, new { memberDetailsId, includeDeleted });
            return rows.AsList();
        }

        public async Task<long> CreateAuthAsync(CreateAuthRequest req, int userId)
        {
            const string sql = @"
                insert into authdetail
                    (authnumber, authtypeid, memberdetailsid, authduedate, nextreviewdate, treatementtype,
                     data, createdon, createdby, authclassid, authassignedto, authstatus)
                values
                    (@authNumber, @authTypeId, @memberDetailsId, @authDueDate, @nextReviewDate, @treatementType,
                     @jsonData::jsonb, now(), @userId, @authClassId, @authAssignedTo, @authStatus)
                returning authdetailid;";

            await using var conn = CreateConn();
            var id = await conn.ExecuteScalarAsync<long>(sql, new
            {
                authNumber = req.AuthNumber,
                authTypeId = req.AuthTypeId,
                memberDetailsId = req.MemberDetailsId,
                authDueDate = req.AuthDueDate,
                nextReviewDate = req.NextReviewDate,
                treatementType = req.TreatementType,
                jsonData = req.JsonData,
                authClassId = req.AuthClassId,
                authAssignedTo = req.AuthAssignedTo,
                authStatus = req.AuthStatus,
                userId
            });

            return id;
        }

        public async Task UpdateAuthAsync(long authDetailId, UpdateAuthRequest req, int userId)
        {
            // only update provided fields; keep data same if JsonData is null
            const string sql = @"
                update authdetail
                set authtypeid     = coalesce(@authTypeId, authtypeid),
                    authduedate     = coalesce(@authDueDate, authduedate),
                    nextreviewdate  = coalesce(@nextReviewDate, nextreviewdate),
                    treatementtype  = coalesce(@treatementType, treatementtype),
                    authclassid     = coalesce(@authClassId, authclassid),
                    authassignedto  = coalesce(@authAssignedTo, authassignedto),
                    authstatus      = coalesce(@authStatus, authstatus),
                    data            = case when @jsonData is null then data else @jsonData::jsonb end,
                    updatedon       = now(),
                    updatedby       = @userId
                where authdetailid = @authDetailId
                  and deletedon is null;";

            await using var conn = CreateConn();
            await conn.ExecuteAsync(sql, new
            {
                authDetailId,
                authTypeId = req.AuthTypeId,
                authDueDate = req.AuthDueDate,
                nextReviewDate = req.NextReviewDate,
                treatementType = req.TreatementType,
                authClassId = req.AuthClassId,
                authAssignedTo = req.AuthAssignedTo,
                authStatus = req.AuthStatus,
                jsonData = req.JsonData,
                userId
            });
        }

        public async Task SoftDeleteAuthAsync(long authDetailId, int userId)
        {
            const string sql = @"
                update authdetail
                set deletedon = now(),
                    deletedby = @userId
                where authdetailid = @authDetailId
                  and deletedon is null;";

            await using var conn = CreateConn();
            await conn.ExecuteAsync(sql, new { authDetailId, userId });
        }
    }
}
