using CareNirvana.Service.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IMemberEnrollmentRepository
    {
        Task<List<MemberEnrollment>> GetMemberEnrollment(int memberdetailsId);
    }
}
