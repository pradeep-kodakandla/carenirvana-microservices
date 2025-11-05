using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class MessageThread
    {
        public long ThreadId { get; set; }
        public int User1Id { get; set; }
        public int User2Id { get; set; }
        public int? MemberDetailsId { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTimeOffset? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public bool ActiveFlag { get; set; }
    }

    public class UserMessage
    {
        public long MessageId { get; set; }
        public long ThreadId { get; set; }
        public long? ParentMessageId { get; set; }
        public int SenderUserId { get; set; }
        public string Body { get; set; } = "";
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset? EditedOn { get; set; }
        public int? EditedBy { get; set; }
    }

    public class CreateMessageRequest
    {
        public int CreatedUserId { get; set; }
        public int OtherUserId { get; set; }          // required
        public int? MemberDetailsId { get; set; }     // optional
        public long? ParentMessageId { get; set; }    // optional reply
        public string Body { get; set; } = "";
    }

    public class UpdateMessageRequest
    {
        public string Body { get; set; } = "";
        public int CreatedUserId { get; set; }
    }

    public class MessageDto
    {
        public long MessageId { get; set; }
        public long ThreadId { get; set; }
        public long? ParentMessageId { get; set; }
        public int SenderUserId { get; set; }
        public string Body { get; set; } = "";
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset? EditedOn { get; set; }
        public IEnumerable<MessageDto>? Replies { get; set; }
    }

    public class ThreadWithMessagesDto
    {
        public long ThreadId { get; set; }
        public int User1Id { get; set; }
        public int User2Id { get; set; }
        public int? MemberDetailsId { get; set; }
        public IEnumerable<MessageDto> Messages { get; set; } = Array.Empty<MessageDto>();
    }

}
