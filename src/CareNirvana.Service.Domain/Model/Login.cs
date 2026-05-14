namespace CareNirvana.Service.Domain.Model
{
    public class Login
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        // Client-reported context (don't fully trust)
        public string? IpAddress { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? LocationAccuracy { get; set; }
    }

    public class LoginAttemptContext
    {
        public string? ClientReportedIp { get; set; }
        public string? ServerObservedIp { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? LocationAccuracy { get; set; }
        public string? UserAgent { get; set; }
        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    }
}
