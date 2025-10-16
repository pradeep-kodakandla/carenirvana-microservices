using CareNirvana.Service.Domain.Model;
using static CareNirvana.Service.Domain.Model.MemberJourney;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IMemberJourney
    {
        Task<(Domain.Model.MemberJourney.PagedResult<MemberJourneyEvent> Page, MemberJourneySummary Summary)> GetMemberJourneyAsync(MemberJourneyRequest request);
    }
}
