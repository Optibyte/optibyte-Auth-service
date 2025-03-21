
using InfluxDB.Client.Core.Flux.Domain;

namespace AuthService.Services.Interfaces
{
    public interface ILoginService
    {
        Task<Dictionary<string, object>> GetUserByEmail(string email);

        string GenerateJwtToken(Dictionary<string, object> data);
    }
}
