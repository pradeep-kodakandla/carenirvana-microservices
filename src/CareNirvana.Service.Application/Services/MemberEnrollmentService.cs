using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Application.Services
{
    public class MemberEnrollmentService:IMemberEnrollmentRepository
    {
        private readonly IMemberEnrollmentRepository _repo;

        public MemberEnrollmentService(IMemberEnrollmentRepository repo)
        {
            _repo = repo;
        }

        public Task<List<MemberEnrollment>> GetMemberEnrollment(int memberdetailsId) => _repo.GetMemberEnrollment(memberdetailsId);

    }
}
