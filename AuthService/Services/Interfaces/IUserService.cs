using AuthService.Models;

namespace AuthService.Services.Interfaces
{
    public interface IUserService
    {
        Task<bool> CreateUser(UserDataRequest user, bool isUpdate);
        Task<List<UserDataResponse>> GetAllUsers(string type, int pageNumber);
        Task<UserDataResponse> GetUserById(Guid userId);
        Task<bool> UpdateUser(UserDataRequest user, Guid userId, HttpContext httpContext);
        Task<bool> DeleteUser(Guid userId, HttpContext httpContext);
        Task<bool> CheckIfEmailExists(string email);
    }
}
