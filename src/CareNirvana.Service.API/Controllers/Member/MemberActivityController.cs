using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace CareNirvana.Service.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberActivityController : ControllerBase
    {
        private readonly IMemberActivity _repository;

        public MemberActivityController(IMemberActivity repository)
        {
            _repository = repository;
        }

        #region DTOs

        public class CreateMemberActivityRequest
        {
            public int? ActivityTypeId { get; set; }
            public int? PriorityId { get; set; }
            public int? MemberDetailsId { get; set; }
            public DateTime? FollowUpDateTime { get; set; }
            public DateTime? DueDate { get; set; }
            public int? ReferTo { get; set; }
            public bool? IsWorkBasket { get; set; }
            public int? QueueId { get; set; }
            public string Comment { get; set; }
            public int? StatusId { get; set; }
            public DateTime? PerformedDateTime { get; set; }
            public int? PerformedBy { get; set; }
            public bool? ActiveFlag { get; set; }

            // Work group pool assignment
            public int? WorkGroupWorkBasketId { get; set; }

            // Audit
            public int CreatedBy { get; set; }
        }

        public class UpdateMemberActivityRequest
        {
            public int MemberActivityId { get; set; }
            public int? ActivityTypeId { get; set; }
            public int? PriorityId { get; set; }
            public int? MemberDetailsId { get; set; }
            public DateTime? FollowUpDateTime { get; set; }
            public DateTime? DueDate { get; set; }
            public int? QueueId { get; set; }
            public string Comment { get; set; }
            public int? StatusId { get; set; }
            public DateTime? PerformedDateTime { get; set; }
            public int? PerformedBy { get; set; }
            public bool? ActiveFlag { get; set; }

            // Audit
            public int UpdatedBy { get; set; }
        }

        public class AcceptWorkGroupActivityRequest
        {
            public int MemberActivityWorkGroupId { get; set; }
            public int UserId { get; set; }
            public string Comment { get; set; }
        }

        public class RejectWorkGroupActivityRequest
        {
            public int MemberActivityWorkGroupId { get; set; }
            public int UserId { get; set; }
            public string Comment { get; set; }
        }

        public class DeleteMemberActivityRequest
        {
            public int MemberActivityId { get; set; }
            public int DeletedBy { get; set; }
        }

        #endregion

        #region Create

        [HttpPost("create")]
        public async Task<IActionResult> CreateMemberActivityAsync(
            [FromBody] CreateMemberActivityRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var activity = new MemberActivity
            {
                ActivityTypeId = request.ActivityTypeId,
                PriorityId = request.PriorityId,
                MemberDetailsId = request.MemberDetailsId,
                FollowUpDateTime = request.FollowUpDateTime,
                DueDate = request.DueDate,
                ReferTo = request.ReferTo,
                IsWorkBasket = request.IsWorkBasket,
                QueueId = request.QueueId,
                Comment = request.Comment,
                StatusId = request.StatusId,
                PerformedDateTime = request.PerformedDateTime,
                PerformedBy = request.PerformedBy,
                ActiveFlag = request.ActiveFlag
            };

            var newId = await _repository.CreateMemberActivityAsync(
                activity,
                request.WorkGroupWorkBasketId,
                request.CreatedBy,
                cancellationToken);

            return Ok(new { memberActivityId = newId });
        }

        #endregion

        #region Update

        [HttpPut("update")]
        public async Task<IActionResult> UpdateMemberActivityAsync(
            [FromBody] UpdateMemberActivityRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var activity = new MemberActivity
            {
                MemberActivityId = request.MemberActivityId,
                ActivityTypeId = request.ActivityTypeId,
                PriorityId = request.PriorityId,
                MemberDetailsId = request.MemberDetailsId,
                FollowUpDateTime = request.FollowUpDateTime,
                DueDate = request.DueDate,
                QueueId = request.QueueId,
                Comment = request.Comment,
                StatusId = request.StatusId,
                PerformedDateTime = request.PerformedDateTime,
                PerformedBy = request.PerformedBy,
                ActiveFlag = request.ActiveFlag
            };

            var affected = await _repository.UpdateMemberActivityAsync(
                activity,
                request.UpdatedBy,
                cancellationToken);

            if (affected == 0)
                return NotFound(new { message = "Member activity not found or already deleted." });

            return Ok(new { affectedRows = affected });
        }

        #endregion

        #region Accept / Reject Work Group Activity

        [HttpPost("accept")]
        public async Task<IActionResult> AcceptWorkGroupActivityAsync(
            [FromBody] AcceptWorkGroupActivityRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var affected = await _repository.AcceptWorkGroupActivityAsync(
                request.MemberActivityWorkGroupId,
                request.UserId,
                request.Comment,
                cancellationToken);

            if (affected == 0)
            {
                // Someone else already took it
                return Conflict(new { message = "Activity has already been accepted by another user." });
            }

            return Ok(new { affectedRows = affected });
        }

        [HttpPost("reject")]
        public async Task<IActionResult> RejectWorkGroupActivityAsync(
            [FromBody] RejectWorkGroupActivityRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var affected = await _repository.RejectWorkGroupActivityAsync(
                request.MemberActivityWorkGroupId,
                request.UserId,
                request.Comment,
                cancellationToken);

            return Ok(new { affectedRows = affected });
        }

        #endregion

        #region Delete (Soft Delete)

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteMemberActivityAsync(
            [FromBody] DeleteMemberActivityRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var affected = await _repository.DeleteMemberActivityAsync(
                request.MemberActivityId,
                request.DeletedBy,
                cancellationToken);

            if (affected == 0)
                return NotFound(new { message = "Member activity not found or already deleted." });

            return Ok(new { affectedRows = affected });
        }
        #endregion

        [HttpGet("requests")]
        public async Task<IActionResult> GetRequestActivitiesAsync(
            [FromQuery] List<int> workGroupWorkBasketIds,
            [FromQuery] DateTime? fromFollowUpDate,
            [FromQuery] DateTime? toFollowUpDate,
            [FromQuery] int? memberDetailsId,
            CancellationToken cancellationToken)
        {
            if (workGroupWorkBasketIds == null || workGroupWorkBasketIds.Count == 0)
                return BadRequest(new { message = "At least one workGroupWorkBasketId is required." });

            var items = await _repository.GetRequestActivitiesAsync(
                workGroupWorkBasketIds,
                fromFollowUpDate,
                toFollowUpDate,
                memberDetailsId,
                cancellationToken);

            return Ok(items);
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentActivitiesAsync(
            [FromQuery] List<int> userIds,
            [FromQuery] DateTime? fromFollowUpDate,
            [FromQuery] DateTime? toFollowUpDate,
            [FromQuery] int? memberDetailsId,
            CancellationToken cancellationToken)
        {
            if (userIds == null || userIds.Count == 0)
                return BadRequest(new { message = "At least one userId is required." });

            var items = await _repository.GetCurrentActivitiesAsync(
                userIds,
                fromFollowUpDate,
                toFollowUpDate,
                memberDetailsId,
                cancellationToken);

            return Ok(items);
        }
        [HttpGet("{memberActivityId}")]
        public async Task<IActionResult> GetMemberActivityDetailAsync( [FromRoute] int memberActivityId, CancellationToken cancellationToken)
        {
            var item = await _repository.GetMemberActivityDetailAsync(
                memberActivityId,
                cancellationToken);
            if (item == null)
                return NotFound(new { message = "Member activity not found." });
            return Ok(item);

        }
    }
}
