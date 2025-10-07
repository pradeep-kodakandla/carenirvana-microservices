using CareNirvana.Service.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Application.Interfaces
{
    public interface IMemberAlertRepository
    {
        Task<MemberAlertPagedResult> GetAlertsAsync(
            int[]? memberDetailsIds = null,
            int? alertId = null,
            bool activeOnly = true,
            int page = 1,
            int pageSize = 50);
        Task<int?> UpdateAlertStatusAsync(
            int memberAlertId,
            int? alertStatusId = null,
            DateTime? dismissedDate = null,
            DateTime? acknowledgedDate = null,
            int updatedBy = 0);
    }


}
