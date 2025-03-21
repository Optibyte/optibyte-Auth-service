using InfluxDB.Client.Writes;
using AuthService.Models;
using AuthService.Services;
using AuthService.Exceptions;


namespace AuthService.Utilities
{
    public static class UserPointBuilder
    {
        public static PointData Build(UserDataResponse user, bool isUpdate)
        {
            try
            {
                var userId = Guid.NewGuid().ToString();
                if (isUpdate)
                {
                    userId = user.UserId.ToString();

                }
                else
                {
                    userId = Guid.NewGuid().ToString();
                }
                var createdAt = user.CreatedAt == default ? DateTime.UtcNow : user.CreatedAt;
                var updatedAt = DateTime.UtcNow.ToString("o");

                var point = PointData.Measurement("Users")
                    .Tag("UserId", userId)
                    .Tag("Type", user.Type ?? string.Empty)
                    .Field("Name", user.Name ?? string.Empty)
                    .Field("Email", user.Email ?? string.Empty)
                    .Field("Password", user.Password ?? string.Empty)
                    .Field("serviceType", user.serviceType ?? string.Empty)
                    .Field("CreatedAt", createdAt.ToString("o"))
                    .Field("UpdatedAt", updatedAt)
                    .Field("Company", user.Type == "SuperAdmin" ? " " : user.Company ?? " ")
                    .Field("CompanyId", user.Type == "SuperAdmin" ? " " : user.CompanyId ?? " ")
                    .Field("Bucket", user.Type == "SuperAdmin" ? " " : user.Bucket ?? " ")
                    .Field("Role", user.Type == "SuperAdmin" || user.Type == "Admin" ? " " : user.Role ?? " ")
                    .Field("Zone", user.Type == "SuperAdmin" || user.Type == "Admin" ? " " : user.Zone ?? " ")
                    .Field("ZoneId", user.Type == "SuperAdmin" || user.Type == "Admin" ? " " : user.ZoneId ?? " ")
                    .Field("Site", user.Type == "SuperAdmin" || user.Type == "Admin" ? " " : user.Site ?? " ")
                    .Field("SiteId", user.Type == "SuperAdmin" || user.Type == "Admin" ? " " : user.SiteId ?? " ");

                return point;
            }
            catch (Exception ex)
            {
                throw new UserServiceException("An error occurred while creating user.");
            }

        }
    }
}
