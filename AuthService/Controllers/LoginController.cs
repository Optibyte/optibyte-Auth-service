using Microsoft.AspNetCore.Mvc;
using AuthService.Models;
using AuthService.Services.Interfaces;
using AuthService.Exceptions;


namespace AuthService.Controllers
{
    [ApiController]
    [Route("authservice/v1/")]
    public class LoginController : ControllerBase
    {
        private readonly ILoginService _loginService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginController> _logger;

        public LoginController(ILoginService loginService, IConfiguration configuration, ILogger<LoginController> logger)
        {
            _loginService = loginService;
            _configuration = configuration;
            _logger = logger;

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Login request)
        {
            try
            {
                 var userData = await _loginService.GetUserByEmail(request.Email);

                if (userData == null)
                {
                    return NotFound(new { Status = 0, Message = "Email not found!" });
                }

                Console.WriteLine($"Extracted Records: {userData}");


                if (userData["Password"].ToString() != request.Password)
                {
                    return BadRequest(new { Status = 0, Message = "Passwords do not match!" });
                }

                var token = _loginService.GenerateJwtToken(userData);
                return Ok(new { Status = 1, Data = new { Token = token } });
            }
            catch (UserServiceException ex)
            {
                return StatusCode(500, new { Status = 0, Message = ex });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = 0, Message = "An unexpected error occurred while logging in." });
            }
        }
    }
}
