using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AuthService.Utilities
{
    public static class AuthorizationHelper
    {
        public static bool AuthorizeUser(HttpContext httpContext, string type, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                // Ensure the Authorization header exists
                if (!httpContext.Request.Headers.ContainsKey("Authorization"))
                {
                    errorMessage = "Authorization header is missing.";
                    return false;
                }

                // Get the token from the Authorization header
                var token = httpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
       
                if (string.IsNullOrEmpty(token))
                {
                    errorMessage = "Authorization token is missing.";
                    return false;
                }

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                if (jwtToken == null)
                {
                    errorMessage = "Invalid token.";
                    return false;
                }

                var payloadType = jwtToken.Claims.FirstOrDefault(c => c.Type == "Type")?.Value;
                Console.WriteLine(payloadType);
                if (string.IsNullOrEmpty(payloadType))
                {
                    errorMessage = "Type claim is missing in token.";
                    return false;
                }

                // Authorization logic
                if (payloadType == "SuperAdmin")
                {
                    return true; 
                }

                if (payloadType == "Admin")
                {

                    if (type == "SuperAdmin" || type == "Admin")
                    {
                        errorMessage = "Admins cannot access SuperAdmin or Admin data.";
                        return false;
                    }

                    return true; 
                }

                if (payloadType == "Employee")
                {
                    errorMessage = "Employees are not authorized.";
                    return false;
                }

                errorMessage = "Unauthorized access.";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = "An error occurred while authorizing the user.";
                return false;
            }
        }
    }
}
