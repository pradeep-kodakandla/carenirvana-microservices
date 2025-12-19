using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace CareNirvana.Service.Infrastructure.Repository
{
    public class CaseRepository : ICaseRepository
    {
        private readonly string _connStr;

        public CaseRepository(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection CreateConn() => new NpgsqlConnection(_connStr);
        public async Task<CaseAggregate?> GetCaseByNumberAsync(string caseNumber, bool includeDeleted = false)
        {
            const string headerSql = @"
                select h.caseheaderid  as CaseHeaderId,
                       h.casenumber    as CaseNumber,
                       h.casetype      as CaseType,
                       h.status        as Status,
                       h.memberdetailid as MemberDetailId,
                       h.createdon     as CreatedOn,
                       h.createdby     as CreatedBy,
                       h.updatedon     as UpdatedOn,
                       h.updatedby     as UpdatedBy,
                       h.deletedon     as DeletedOn,
                       h.deletedby     as DeletedBy,
                       su.username     as CreatedByUserName,
                       md.memberid     as MemberId,
                       (coalesce(md.firstname,'') || case when md.lastname is null or md.lastname = '' then '' else ' ' || md.lastname end) as MemberName
                from caseheader h
                left join securityuser su on su.userid = h.createdby
                left join memberdetails md on md.memberdetailsid = h.memberdetailid
                where h.casenumber = @caseNumber
                  and (@includeDeleted = true or h.deletedon is null);";

            const string detailsSql = @"
                select d.casedetailid    as CaseDetailId,
                       d.caseheaderid    as CaseHeaderId,
                       d.caselevelid     as CaseLevelId,
                       d.caselevelnumber as CaseLevelNumber,
                       d.jsondata::text  as JsonData,
                       d.createdon       as CreatedOn,
                       d.createdby       as CreatedBy,
                       d.updatedon       as UpdatedOn,
                       d.updatedby       as UpdatedBy,
                       d.deletedon       as DeletedOn,
                       d.deletedby       as DeletedBy
                from casedetail d
                join caseheader h on h.caseheaderid = d.caseheaderid
                where h.casenumber = @caseNumber
                  and (@includeDeleted = true or d.deletedon is null)
                order by d.caselevelid;";

            await using var conn = CreateConn();

            var header = await conn.QueryFirstOrDefaultAsync<CaseHeader>(
                headerSql, new { caseNumber, includeDeleted });

            if (header == null) return null;

            var details = (await conn.QueryAsync<CaseDetail>(
                detailsSql, new { caseNumber, includeDeleted })).AsList();

            return new CaseAggregate { Header = header, Details = details };
        }


        public async Task<CaseAggregate?> GetCaseByHeaderIdAsync(long caseHeaderId, bool includeDeleted = false)
        {
            const string headerSql = @"
                select h.caseheaderid   as CaseHeaderId,
                       h.casenumber     as CaseNumber,
                       h.casetype       as CaseType,
                       h.status         as Status,
                       h.memberdetailid as MemberDetailId,
                       h.createdon      as CreatedOn,
                       h.createdby      as CreatedBy,
                       h.updatedon      as UpdatedOn,
                       h.updatedby      as UpdatedBy,
                       h.deletedon      as DeletedOn,
                       h.deletedby      as DeletedBy,
                       su.username      as CreatedByUserName,
                       md.memberid      as MemberId,
                       (coalesce(md.firstname,'') || case when md.lastname is null or md.lastname = '' then '' else ' ' || md.lastname end) as MemberName
                from caseheader h
                left join securityuser su on su.userid = h.createdby
                left join memberdetails md on md.memberdetailsid = h.memberdetailid
                where h.caseheaderid = @caseHeaderId
                  and (@includeDeleted = true or h.deletedon is null);";

            const string detailsSql = @"
                select d.casedetailid    as CaseDetailId,
                       d.caseheaderid    as CaseHeaderId,
                       d.caselevelid     as CaseLevelId,
                       d.caselevelnumber as CaseLevelNumber,
                       d.jsondata::text  as JsonData,
                       d.createdon       as CreatedOn,
                       d.createdby       as CreatedBy,
                       d.updatedon       as UpdatedOn,
                       d.updatedby       as UpdatedBy,
                       d.deletedon       as DeletedOn,
                       d.deletedby       as DeletedBy
                from casedetail d
                where d.caseheaderid = @caseHeaderId
                  and (@includeDeleted = true or d.deletedon is null)
                order by d.caselevelid;";

            await using var conn = CreateConn();

            var header = await conn.QueryFirstOrDefaultAsync<CaseHeader>(
                headerSql, new { caseHeaderId, includeDeleted });

            if (header == null) return null;

            var details = (await conn.QueryAsync<CaseDetail>(
                detailsSql, new { caseHeaderId, includeDeleted })).AsList();

            return new CaseAggregate { Header = header, Details = details };
        }

        public async Task<List<CaseAggregate>> GetCasesByMemberDetailIdAsync(
            long memberDetailId,
            bool includeDetails = false,
            IEnumerable<string>? statuses = null,
            bool includeDeleted = false)
        {
            // IMPORTANT: Postgres array filtering uses = any(@statuses)
            // Dapper will send string[] properly.
            var statusArr = statuses?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToArray();

            const string headersSql = @"
                select h.caseheaderid   as CaseHeaderId,
                       h.casenumber     as CaseNumber,
                       h.casetype       as CaseType,
                       h.status         as Status,
                       h.memberdetailid as MemberDetailId,
                       h.createdon      as CreatedOn,
                       h.createdby      as CreatedBy,
                       h.updatedon      as UpdatedOn,
                       h.updatedby      as UpdatedBy,
                       h.deletedon      as DeletedOn,
                       h.deletedby      as DeletedBy,

                       su.username      as CreatedByUserName,
                       md.memberid      as MemberId,
                       (coalesce(md.firstname,'') || case when md.lastname is null or md.lastname = '' then '' else ' ' || md.lastname end) as MemberName
                from caseheader h
                left join securityuser su on su.userid = h.createdby
                left join memberdetails md on md.memberdetailsid = h.memberdetailid
                where h.memberdetailid = @memberDetailId
                  and (@includeDeleted = true or h.deletedon is null)
                  and (
                        @statusArr is null
                        or cardinality(@statusArr) = 0
                        or h.status = any(@statusArr)
                      )
                order by h.createdon desc;";

            const string detailsByHeaderSql = @"
                select d.casedetailid    as CaseDetailId,
                       d.caseheaderid    as CaseHeaderId,
                       d.caselevelid     as CaseLevelId,
                       d.caselevelnumber as CaseLevelNumber,
                       d.jsondata::text  as JsonData,
                       d.createdon       as CreatedOn,
                       d.createdby       as CreatedBy,
                       d.updatedon       as UpdatedOn,
                       d.updatedby       as UpdatedBy,
                       d.deletedon       as DeletedOn,
                       d.deletedby       as DeletedBy
                from casedetail d
                where d.caseheaderid = @caseHeaderId
                  and (@includeDeleted = true or d.deletedon is null)
                order by d.caselevelid;";

            await using var conn = CreateConn();

            var headers = (await conn.QueryAsync<CaseHeader>(
                headersSql, new { memberDetailId, includeDetails, includeDeleted, statusArr })).AsList();

            if (!includeDetails)
                return headers.Select(h => new CaseAggregate { Header = h, Details = new List<CaseDetail>() }).ToList();

            // load details per header (simple + clear). If you want, I can optimize into a single query.
            var result = new List<CaseAggregate>(headers.Count);

            foreach (var h in headers)
            {
                var details = (await conn.QueryAsync<CaseDetail>(
                    detailsByHeaderSql, new { caseHeaderId = h.CaseHeaderId, includeDeleted })).AsList();

                result.Add(new CaseAggregate { Header = h, Details = details });
            }

            return result;
        }

        /// First insert: insert caseheader + insert casedetail using LevelId (assumed to be 1 for Level1)
        public async Task<CreateCaseResult> CreateCaseAsync(CreateCaseRequest req, long userId)
        {
            const string findHeaderSql = @"
                select caseheaderid as CaseHeaderId,
                       casenumber   as CaseNumber,
                       memberdetailid as MemberDetailId
                from caseheader
                where casenumber = @caseNumber and deletedon is null;";

            const string insertHeaderSql = @"
                insert into caseheader (casenumber, casetype, status,memberdetailid, createdon, createdby)
                values (@caseNumber, @caseType, @status, @memberDetailId, now(), @userId)
                returning caseheaderid as CaseHeaderId, casenumber as CaseNumber;";

            const string insertDetailSql = @"
                insert into casedetail (caseheaderid, caselevelid, caselevelnumber, jsondata, createdon, createdby)
                values (@caseHeaderId, @caseLevelId, @caseLevelNumber, @jsonData::jsonb, now(), @userId)
                returning casedetailid;";

            await using var conn = CreateConn();
            await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            var existing = await conn.QueryFirstOrDefaultAsync<CaseHeader>(
                findHeaderSql,
                new { caseNumber = req.CaseNumber },
                tx);

            long caseHeaderId;
            string caseNumber;

            if (existing != null)
            {
                caseHeaderId = existing.CaseHeaderId;
                caseNumber = existing.CaseNumber;
            }
            else
            {
                var inserted = await conn.QuerySingleAsync<CaseHeader>(
                    insertHeaderSql,
                    new
                    {
                        caseNumber = req.CaseNumber,
                        caseType = req.CaseType,
                        status = req.Status,
                        memberDetailId = req.MemberDetailId,
                        userId
                    },
                    tx);

                caseHeaderId = inserted.CaseHeaderId;
                caseNumber = inserted.CaseNumber;
            }

            // Example: 51471L1, 51471L2
            var caseLevelNumber = $"{caseNumber}L{req.LevelId}";

            var caseDetailId = await conn.ExecuteScalarAsync<long>(
                insertDetailSql,
                new
                {
                    caseHeaderId,
                    caseLevelId = req.LevelId,
                    caseLevelNumber,
                    jsonData = req.JsonData,
                    userId
                },
                tx);

            await tx.CommitAsync();

            return new CreateCaseResult
            {
                CaseHeaderId = caseHeaderId,
                CaseNumber = caseNumber,
                CaseDetailId = caseDetailId,
                CaseLevelNumber = caseLevelNumber
            };
        }

        public async Task<AddLevelResult> AddCaseLevelAsync(AddCaseLevelRequest req, long userId)
        {
            const string headerSql = @"
                select casenumber
                from caseheader
                where caseheaderid = @caseHeaderId and deletedon is null;";

            const string insertDetailSql = @"
                insert into casedetail (caseheaderid, caselevelid, caselevelnumber, jsondata, createdon, createdby)
                values (@caseHeaderId, @caseLevelId, @caseLevelNumber, @jsonData::jsonb, now(), @userId)
                returning casedetailid;";

            await using var conn = CreateConn();
            await conn.OpenAsync();

            var caseNumber = await conn.ExecuteScalarAsync<string?>(
                headerSql,
                new { caseHeaderId = req.CaseHeaderId });

            if (string.IsNullOrWhiteSpace(caseNumber))
                throw new InvalidOperationException($"Case header not found for CaseHeaderId={req.CaseHeaderId}");

            var caseLevelNumber = $"{caseNumber}L{req.LevelId}";

            var caseDetailId = await conn.ExecuteScalarAsync<long>(
                insertDetailSql,
                new
                {
                    caseHeaderId = req.CaseHeaderId,
                    caseLevelId = req.LevelId,
                    caseLevelNumber,
                    jsonData = req.JsonData,
                    userId
                });

            return new AddLevelResult
            {
                CaseHeaderId = req.CaseHeaderId,
                CaseNumber = caseNumber,
                CaseDetailId = caseDetailId,
                CaseLevelNumber = caseLevelNumber
            };
        }

        /// Update only casedetail
        public async Task UpdateCaseDetailAsync(UpdateCaseDetailRequest req, long userId)
        {
            Console.WriteLine($"Updating CaseDetailId={req.CaseDetailId} by UserId={userId}");
            Console.WriteLine($"New JsonData: {req.JsonData}");
            const string sql = @"
                update casedetail
                set jsondata    = @jsonData::jsonb,
                    caselevelid = coalesce(@caseLevelId, caselevelid),
                    updatedon   = now(),
                    updatedby   = @userId
                where casedetailid = @caseDetailId
                  and deletedon is null;";

            await using var conn = CreateConn();
            await conn.ExecuteAsync(sql, new
            {
                caseDetailId = req.CaseDetailId,
                jsonData = req.JsonData,
                caseLevelId = req.LevelId,
                userId
            });
        }

        public async Task SoftDeleteCaseHeaderAsync(long caseHeaderId, long userId, bool cascadeDetails = true)
        {
            const string headerSql = @"
                update caseheader
                set deletedon = now(), deletedby = @userId
                where caseheaderid = @caseHeaderId
                  and deletedon is null;";

            const string detailsSql = @"
                update casedetail
                set deletedon = now(), deletedby = @userId
                where caseheaderid = @caseHeaderId
                  and deletedon is null;";

            await using var conn = CreateConn();
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            await conn.ExecuteAsync(headerSql, new { caseHeaderId, userId }, tx);

            if (cascadeDetails)
                await conn.ExecuteAsync(detailsSql, new { caseHeaderId, userId }, tx);

            await tx.CommitAsync();
        }

        public async Task SoftDeleteCaseDetailAsync(long caseDetailId, long userId)
        {
            const string sql = @"
                update casedetail
                set deletedon = now(), deletedby = @userId
                where casedetailid = @caseDetailId
                  and deletedon is null;";

            await using var conn = CreateConn();
            await conn.ExecuteAsync(sql, new { caseDetailId, userId });
        }
    }
}