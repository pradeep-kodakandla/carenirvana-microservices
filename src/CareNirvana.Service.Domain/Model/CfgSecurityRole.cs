namespace CareNirvana.Service.Domain.Model
{
    public class CfgSecurityRole
    {
        public int RoleId { get; set; }

        public string RoleName { get; set; }

        public bool IsManager { get; set; }

        public bool IsSensitiveData { get; set; }

        public bool IsQoc { get; set; }

        public bool ActiveFlag { get; set; }
    }
}
