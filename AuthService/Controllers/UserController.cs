using AuthService.Models;
using AuthService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using AuthService.Utilities;
using InfluxDB.Client.Api.Domain;
using AuthService.Exceptions;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("authservice/v1/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // Creates a new user.
        [HttpPost]
        public async Task<IActionResult> Createuser([FromBody] UserDataRequest user)
        {
            if (!AuthorizationHelper.AuthorizeUser(HttpContext, user.Type, out var errorMessage))
            {
                return BadRequest(new { status = 0, message = errorMessage });
            }
            Console.WriteLine(user);

            if (user == null || user.Type != "SuperAdmin" && user.Type != "Admin" && user.Type != "Employee")
            {
                return BadRequest(new { status = 0, message = "Invalid user data!" });
            }

            try
            {

                var result = await _userService.CreateUser(user, false);
                if (result)
                {
                    return StatusCode(201,  new { status = 1, message = "User created successfully." });
                }
                else
                {
                    return StatusCode(500, new { status = 0, message = "An unexpected error occurred while creating the user." });
                }
            }
            catch (UserServiceException ex)
            {
                return StatusCode(500, new { status = 0, message = ex });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 0, message = "An unexpected error occurred while creating the user." });
            }
        }


        // Retrieves all users.
        [HttpGet("type/{type}")]
        public async Task<IActionResult> GetAllusers(string type, [FromQuery] int pageNumber = 1)
        {
            if (!AuthorizationHelper.AuthorizeUser(HttpContext, type, out var errorMessage))
            {
                return BadRequest(new { status = 0, message = errorMessage });
            }

            if (type == null || type != "SuperAdmin" && type != "Admin" && type != "Employee")
            {
                return BadRequest(new { status = 0, message = "Invalid user data!" });
            }

            try
            {

                var users = await _userService.GetAllUsers(type,pageNumber);

                if (users != null && users.Count > 0)
                {
                    return Ok(new { status = 1, data = users, message = "Users retrieved successfully." });
                }
                else
                {
                    return NotFound(new { status = 0, message = "No users found!" });
                }
            }
            catch (UserServiceException ex)
            {
                return StatusCode(500, new { status = 0, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 0, message = "An unexpected error occurred while getting the users." });
            }
        }


        // Retrieves a user by UserId.
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetuserById(Guid userId)
        {
            if (string.IsNullOrEmpty(userId.ToString()))
            {
                return BadRequest(new { status = 0, message = "Invalid UserId!" });
            }

            try
            {
                var user = await _userService.GetUserById(userId);
                if (!AuthorizationHelper.AuthorizeUser(HttpContext, user.Type, out var errorMessage))
                {
                    return BadRequest(new { status = 0, message = errorMessage });
                }
                if (user != null)
                {
                    return Ok(new { status = 1, data = user, message = "User retrieved successfully." });
                }
                else
                {
                    return NotFound(new { status = 0, message = "User not found!" });
                }
            }
            catch (UserServiceException ex)
            {
                return StatusCode(500, new { status = 0, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 0, message = "An unexpected error occurred while getting the user." });
            }
        }

        // Updates an existing user.
        [HttpPut("{userId}")]
        public async Task<IActionResult> Updateuser(Guid userId, [FromBody] UserDataRequest user)

        {
            if (!AuthorizationHelper.AuthorizeUser(HttpContext, user.Type, out var errorMessage))
            {
                return BadRequest(new { status = 0, message = errorMessage });
            }

            if ( user == null || userId != user.UserId || user.Type != "SuperAdmin" && user.Type != "Admin" && user.Type != "Employee")
            {
                return BadRequest(new { status = 0, message = "Invalid request data!" });
            }

            try
            {
                var result = await _userService.UpdateUser(user, userId, HttpContext);
                if (result)
                {
                    return Ok(new { status = 1, message = "User updated successfully." });
                }
                else
                {
                    return StatusCode(500, new { status = 0, message = "An error occurred while updating the user." });
                }
            }
            catch (UserServiceException ex)
            {
                return StatusCode(500, new { status = 0, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 0, message = "An unexpected error occurred while updating the user." });
            }
        }

        // Deletes a user by UserId.
        [HttpDelete("{userId}")]
        public async Task<IActionResult> Deleteuser(Guid userId)
        {

            if (string.IsNullOrEmpty(userId.ToString()))
            {
                return BadRequest(new { status = 0, message = "Invalid UserId!" });
            }

            try
            {
                var result = await _userService.DeleteUser(userId, HttpContext);
                if (result)
                {
                    return NoContent();
                }
                else
                {
                    return StatusCode(500, new { status = 0, message = "An error occurred while deleting the user." });
                }
            }
            catch (UserServiceException ex)
            {
                return StatusCode(500, new { status = 0, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 0, message = "An unexpected error occurred while deleting the user." });
            }
        }

    }
}
