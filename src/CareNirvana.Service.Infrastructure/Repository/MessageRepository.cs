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
    public class MessageRepository : IMessageRepository
    {
        private readonly string _connectionString;

        public MessageRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public async Task<long> EnsureThreadAsync(int currentUserId, int otherUserId, int? memberDetailsId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            var sqlFind = @"
                SELECT threadid
                FROM public.usermessagethread
                WHERE userlo = LEAST(@u1, @u2)
                  AND userhi = GREATEST(@u1, @u2)
                  AND ((@mid IS NULL AND memberdetailsid IS NULL)
                       OR (@mid IS NOT NULL AND memberdetailsid = @mid))
                LIMIT 1;";
            var existing = await conn.ExecuteScalarAsync<long?>(sqlFind, new { u1 = currentUserId, u2 = otherUserId, mid = memberDetailsId });
            if (existing.HasValue) return existing.Value;

            var sqlInsert = @"
                INSERT INTO public.usermessagethread
                    (user1id, user2id, memberdetailsid, createdby)
                VALUES
                    (@u1, @u2, @mid, @creator)
                RETURNING threadid;";
            return await conn.ExecuteScalarAsync<long>(sqlInsert, new { u1 = currentUserId, u2 = otherUserId, mid = memberDetailsId, creator = currentUserId });
        }

        public async Task<long> CreateMessageAsync(int senderUserId, long threadId, string body, long? parentMessageId, string subject)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            const string sql = @"
                INSERT INTO public.usermessage (threadid, parentmessageid, senderuserid, body, subject)
                VALUES (@tid, @pid, @sid, @body, @subject)
                RETURNING messageid;";
            return await conn.ExecuteScalarAsync<long>(sql, new { tid = threadId, pid = parentMessageId, sid = senderUserId, body, subject });
        }

        public async Task<int> UpdateMessageAsync(long messageId, int editorUserId, string newBody)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            const string sql = @"
                UPDATE public.usermessage
                   SET body = @body,
                       editedon = now(),
                       editedby = @editor
                 WHERE messageid = @id
                   AND isdeleted = false;";
            return await conn.ExecuteAsync(sql, new { id = messageId, body = newBody, editor = editorUserId });
        }

        public async Task<int> DeleteMessageAsync(long messageId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            const string sql = "UPDATE public.usermessage SET isdeleted = true WHERE messageid = @id;";
            return await conn.ExecuteAsync(sql, new { id = messageId });
        }

        public async Task<ThreadWithMessagesDto?> GetThreadAsync(long threadId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            //const string threadSql = "SELECT * FROM public.usermessagethread WHERE threadid = @id;";
            const string threadSql = @"
                SELECT 
                  t.threadid       AS ThreadId,
                  t.user1id        AS User1Id,
                  t.user2id        AS User2Id,
                  t.memberdetailsid AS MemberDetailsId,
                  su1.username     AS User1Name,
                  su2.username     AS User2Name
                FROM public.usermessagethread t
                LEFT JOIN public.securityuser su1 ON su1.userid = t.user1id
                LEFT JOIN public.securityuser su2 ON su2.userid = t.user2id
                WHERE t.threadid = @id;";
            const string msgsSql = @"
                  SELECT 
                      um.messageid      AS MessageId,
                      um.threadid       AS ThreadId,
                      um.parentmessageid AS ParentMessageId,
                      um.senderuserid   AS SenderUserId,
                      um.body           AS Body,
                      um.subject        AS Subject,     -- if you added this column
                      um.isdeleted      AS IsDeleted,
                      um.createdon      AS CreatedOn,
                      um.editedon       AS EditedOn,
                      su.username       AS UserName
                  FROM public.usermessage um
                  JOIN public.securityuser su ON su.userid = um.senderuserid
                  WHERE um.threadid = @id
                  ORDER BY um.createdon DESC;";

            using var multi = await conn.QueryMultipleAsync(threadSql + msgsSql, new { id = threadId });
            var thread = await multi.ReadFirstOrDefaultAsync<MessageThread>();
            if (thread == null) return null;

            var flat = (await multi.ReadAsync<MessageDto>()).ToList();
            var map = flat.ToDictionary(m => m.MessageId);
            foreach (var m in map.Values) m.Replies = new List<MessageDto>();
            var roots = new List<MessageDto>();
            foreach (var m in map.Values)
            {
                if (m.ParentMessageId.HasValue && map.TryGetValue(m.ParentMessageId.Value, out var parent))
                    ((List<MessageDto>)parent.Replies!).Add(m);
                else
                    roots.Add(m);
            }

            return new ThreadWithMessagesDto
            {
                ThreadId = thread.ThreadId,
                User1Id = thread.User1Id,
                User2Id = thread.User2Id,
                User1Name = thread.User1Name, 
                User2Name = thread.User2Name, 
                MemberDetailsId = thread.MemberDetailsId,
                Messages = roots
            };
        }

        public async Task<IEnumerable<ThreadWithMessagesDto>> GetByUserAsync(int userId, int page = 1, int pageSize = 50)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            var threadIds = await conn.QueryAsync<long>(@"
                SELECT threadid
                FROM public.usermessagethread
                WHERE userlo = @uid OR userhi = @uid
                ORDER BY createdon DESC
                OFFSET @off LIMIT @lim;",
                new { uid = userId, off = (page - 1) * pageSize, lim = pageSize });

            var results = new List<ThreadWithMessagesDto>();
            foreach (var t in threadIds)
                results.Add(await GetThreadAsync(t) ?? new ThreadWithMessagesDto { ThreadId = t, Messages = Array.Empty<MessageDto>() });
            return results;
        }

        public async Task<IEnumerable<ThreadWithMessagesDto>> GetByMemberAsync(int memberDetailsId, int page = 1, int pageSize = 50)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            var threadIds = await conn.QueryAsync<long>(@"
                SELECT threadid
                FROM public.usermessagethread
                WHERE memberdetailsid = @mid
                ORDER BY createdon DESC
                OFFSET @off LIMIT @lim;",
                new { mid = memberDetailsId, off = (page - 1) * pageSize, lim = pageSize });

            var results = new List<ThreadWithMessagesDto>();
            foreach (var t in threadIds)
                results.Add(await GetThreadAsync(t) ?? new ThreadWithMessagesDto { ThreadId = t, Messages = Array.Empty<MessageDto>() });
            return results;
        }
    }
}