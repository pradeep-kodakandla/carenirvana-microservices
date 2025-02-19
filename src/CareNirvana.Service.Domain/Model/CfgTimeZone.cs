namespace CareNirvana.Service.Domain.Model
{
    public class CfgTimeZone
    {
        public int TimeZoneId { get; set; }

        public string? TimeZone { get; set; }

        public string? TimeZoneDescription { get; set; }

        public bool ActiveFlag { get; set; }
    }
}
