namespace CareNirvana.Service.Domain.Model
{
    public class SecurityUser
    {
        public int UserId { get; set; }

        public required string? UserName { get; set; }

        public string? Password { get; set; }

        public CfgUserType? UserType { get; set; }

        public SecurityUserDetail? UserDetail { get; set; }

        public bool ActiveFlag { get; set; }

        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? DeletedBy { get; set; }
    }
}

