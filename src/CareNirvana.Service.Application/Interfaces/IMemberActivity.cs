using CareNirvana.Service.Domain.Model;
using System;


namespace CareNirvana.Service.Application.Interfaces
{
    public interface IMemberActivity
    {
        Task<int> CreateMemberActivityAsync(MemberActivity activity, int? workGroupWorkBasketId, int createdBy, CancellationToken cancellationToken = default);
        Task<int> UpdateMemberActivityAsync(MemberActivity activity, int updatedBy, CancellationToken cancellationToken = default);
        Task<int> RejectWorkGroupActivityAsync(int memberActivityWorkGroupId, int userId, string comment, CancellationToken cancellationToken = default);
        Task<int> AcceptWorkGroupActivityAsync(int memberActivityWorkGroupId, int userId, string comment, CancellationToken cancellationToken = default);
        Task<int> UpdateWorkGroupStatusAsync(int memberActivityWorkGroupId, int? groupStatusId, int updatedBy, CancellationToken cancellationToken = default);
        Task<int> DeleteMemberActivityAsync(int memberActivityId, int deletedBy, CancellationToken cancellationToken = default);

        Task<IEnumerable<MemberActivityRequestItem>> GetRequestActivitiesAsync(
                   IEnumerable<int> workGroupWorkBasketIds,
                   DateTime? fromFollowUpDate,
                   DateTime? toFollowUpDate,
                   int? memberDetailsId,
                   CancellationToken cancellationToken);

        Task<IEnumerable<MemberActivityCurrentItem>> GetCurrentActivitiesAsync(
            IEnumerable<int> userIds,
            DateTime? fromFollowUpDate,
            DateTime? toFollowUpDate,
            int? memberDetailsId,
            CancellationToken cancellationToken);
        Task<MemberActivityDetailItem?> GetMemberActivityDetailAsync(int memberActivityId, CancellationToken cancellationToken = default);
    }
}

