using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using static CareNirvana.Service.Domain.Model.MemberJourney;

namespace CareNirvana.Service.Application.Services
{
    internal class MemberJourneyService : IMemberJourney
    {
        private readonly IMemberJourney _repo;
        public MemberJourneyService(IMemberJourney repo)
        {
            _repo = repo;
        }

        public Task<(Domain.Model.MemberJourney.PagedResult<MemberJourneyEvent> Page, MemberJourneySummary Summary)> GetAsync(MemberJourneyRequest request) => _repo.GetMemberJourneyAsync(request);

        async Task<(Domain.Model.MemberJourney.PagedResult<MemberJourneyEvent> Page, MemberJourneySummary Summary)> IMemberJourney.GetMemberJourneyAsync(MemberJourneyRequest request)
        {
            return await GetAsync(request);
        }

    }
}
