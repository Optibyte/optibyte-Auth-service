namespace AuthService.Models
{
    public class UserDataRequest
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Company { get; set; }
        public string Site { get; set; }
        public string Zone { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public string serviceType { get; set; }

        public string Password { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UserDataResponse
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Company { get; set; }
        public string Bucket { get; set; }
        public string CompanyId { get; set; }
        public string Site { get; set; }
        public string SiteId { get; set; }
        public string Zone { get; set; }
        public string serviceType { get; set; }

        public string ZoneId { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CompanyData
    {
        public string Id { get; set; }
        public string Topic { get; set; }
    }

    public class ZoneData
    {
        public string Id { get; set; }
    }

    public class SiteData
    {
        public string Id { get; set; }
    }

}
