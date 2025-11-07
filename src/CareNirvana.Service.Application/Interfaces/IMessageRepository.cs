using CareNirvana.Service.Domain.Model;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IMessageRepository
    {
        Task<long> EnsureThreadAsync(int currentUserId, int otherUserId, int? memberDetailsId);
        Task<long> CreateMessageAsync(int senderUserId, long threadId, string body, long? parentMessageId, string subject);
        Task<int> UpdateMessageAsync(long messageId, int editorUserId, string newBody);
        Task<int> DeleteMessageAsync(long messageId); // soft delete
        Task<IEnumerable<ThreadWithMessagesDto>> GetByUserAsync(int userId, int page = 1, int pageSize = 50);
        Task<IEnumerable<ThreadWithMessagesDto>> GetByMemberAsync(int memberDetailsId, int page = 1, int pageSize = 50);
        Task<ThreadWithMessagesDto?> GetThreadAsync(long threadId);
    }
}
