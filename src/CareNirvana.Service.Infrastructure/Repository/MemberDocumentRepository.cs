using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Infrastructure.Repository
{
    public class MemberDocumentRepository : IMemberDocument
    {
        private readonly string _connectionString;

        public MemberDocumentRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public async Task<long> InsertMemberDocumentAsync(MemberDocument doc)
        {
            const string sql = @"
        INSERT INTO memberdocument
        (memberid, documenttypeid, documentname, documentbytes, createdon, createdby)
        VALUES
        (@memberid, @documenttypeid, @documentname, @documentbytes, COALESCE(@createdon, NOW()), @createdby)
        RETURNING memberdocumentid;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@memberid", doc.MemberId);
            cmd.Parameters.AddWithValue("@documenttypeid", (object?)doc.DocumentTypeId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@documentname", doc.DocumentName);
            cmd.Parameters.AddWithValue("@documentbytes", doc.DocumentBytes);
            cmd.Parameters.AddWithValue("@createdon", (object?)doc.CreatedOn ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@createdby", (object?)doc.CreatedBy ?? DBNull.Value);

            var id = await cmd.ExecuteScalarAsync();
            return (long)id!;
        }

        public async Task<int> UpdateMemberDocumentAsync(MemberDocument doc)
        {
            const string sql = @"
        UPDATE memberdocument
        SET documenttypeid = @documenttypeid,
            documentname   = @documentname,
            documentbytes  = @documentbytes,
            updatedon      = COALESCE(@updatedon, NOW()),
            updatedby      = @updatedby
        WHERE memberdocumentid = @id
          AND deletedon IS NULL;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", doc.MemberDocumentId);
            cmd.Parameters.AddWithValue("@documenttypeid", (object?)doc.DocumentTypeId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@documentname", doc.DocumentName);
            cmd.Parameters.AddWithValue("@documentbytes", doc.DocumentBytes);
            cmd.Parameters.AddWithValue("@updatedon", (object?)doc.UpdatedOn ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updatedby", (object?)doc.UpdatedBy ?? DBNull.Value);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> SoftDeleteMemberDocumentAsync(long memberDocumentId, int deletedBy)
        {
            const string sql = @"
        UPDATE memberdocument
        SET deletedon = NOW(),
            deletedby = @deletedby
        WHERE memberdocumentid = @id
          AND deletedon IS NULL;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", memberDocumentId);
            cmd.Parameters.AddWithValue("@deletedby", deletedBy);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<MemberDocument?> GetMemberDocumentByIdAsync(long id)
        {
            const string sql = @"
        SELECT memberdocumentid, memberid, documenttypeid, documentname, documentbytes,
               createdon, createdby, updatedby, updatedon, deletedby, deletedon
        FROM memberdocument
        WHERE memberdocumentid = @id;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (await reader.ReadAsync())
            {
                return new MemberDocument
                {
                    MemberDocumentId = reader.GetInt64(reader.GetOrdinal("memberdocumentid")),
                    MemberId = reader.GetInt64(reader.GetOrdinal("memberid")),
                    DocumentTypeId = reader.IsDBNull(reader.GetOrdinal("documenttypeid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("documenttypeid")),
                    DocumentName = reader.GetString(reader.GetOrdinal("documentname")),
                    DocumentBytes = (byte[])reader["documentbytes"],
                    CreatedOn = reader.GetDateTime(reader.GetOrdinal("createdon")),
                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("createdby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("createdby")),
                    UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updatedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("updatedby")),
                    UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updatedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("updatedon")),
                    DeletedBy = reader.IsDBNull(reader.GetOrdinal("deletedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("deletedby")),
                    DeletedOn = reader.IsDBNull(reader.GetOrdinal("deletedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("deletedon"))
                };
            }
            return null;
        }

        public async Task<(List<MemberDocument> Items, int Total)> GetMemberDocumentsForMemberAsync(long memberId, int page = 1, int pageSize = 25, bool includeDeleted = false)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 25;

            var results = new List<MemberDocument>();
            var total = 0;
            var filter = includeDeleted ? "" : "AND md.deletedon IS NULL";

            var sql = $@"
        SELECT md.memberdocumentid, md.memberid, md.documenttypeid, md.documentname, md.documentbytes,
               md.createdon, md.createdby, md.updatedby, md.updatedon, md.deletedby, md.deletedon,
               COUNT(*) OVER() AS total_count
        FROM memberdocument md
        WHERE md.memberid = @memberid
          {filter}
        ORDER BY COALESCE(md.updatedon, md.createdon) DESC
        OFFSET @offset LIMIT @limit;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@memberid", memberId);
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@limit", pageSize);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (total == 0 && !reader.IsDBNull(reader.GetOrdinal("total_count")))
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));

                results.Add(new MemberDocument
                {
                    MemberDocumentId = reader.GetInt64(reader.GetOrdinal("memberdocumentid")),
                    MemberId = reader.GetInt64(reader.GetOrdinal("memberid")),
                    DocumentTypeId = reader.IsDBNull(reader.GetOrdinal("documenttypeid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("documenttypeid")),
                    DocumentName = reader.GetString(reader.GetOrdinal("documentname")),
                    DocumentBytes = (byte[])reader["documentbytes"],
                    CreatedOn = reader.GetDateTime(reader.GetOrdinal("createdon")),
                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("createdby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("createdby")),
                    UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updatedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("updatedby")),
                    UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updatedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("updatedon")),
                    DeletedBy = reader.IsDBNull(reader.GetOrdinal("deletedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("deletedby")),
                    DeletedOn = reader.IsDBNull(reader.GetOrdinal("deletedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("deletedon"))
                });
            }

            return (results, total);
        }


    }
}
