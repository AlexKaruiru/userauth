using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using userauth.DTOs;
using userauth.Interfaces;

namespace userauth.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // POST: api/Users/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _userService.RegisterUserAsync(model);

            if (result.Succeeded)
            {
                return Ok(new { Message = "User registered successfully." });
            }

            return BadRequest(result.Errors);
        }

        // POST: api/Users/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var token = await _userService.LoginUserAsync(model);

            if (token == null)
            {
                return Unauthorized(new { Message = "Invalid login credentials." });
            }

            return Ok(new { Token = token });
        }

        // GET: api/Users/profile
        [HttpGet("profile")]
        [Authorize] // Requires authentication
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get user ID from JWT claims
            if (userId == null)
            {
                return Unauthorized(new { Message = "User not found in claims." });
            }

            var userProfile = await _userService.GetUserProfileAsync(userId);
            if (userProfile == null)
            {
                return NotFound(new { Message = "User profile not found." });
            }

            return Ok(userProfile);
        }

        // PUT: api/Users/profile
        [HttpPut("profile")]
        [Authorize] // Requires authentication
        public async Task<IActionResult> UpdateProfile([FromBody] UserUpdateProfileDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized(new { Message = "User not found in claims." });
            }

            var result = await _userService.UpdateUserProfileAsync(userId, model);

            if (result.Succeeded)
            {
                return Ok(new { Message = "Profile updated successfully." });
            }

            return BadRequest(result.Errors);
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Only accessible by users with "Admin" role
        public async Task<IActionResult> DeleteUser(string id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (result.Succeeded)
            {
                return Ok(new { Message = "User deleted successfully." });
            }
            return BadRequest(result.Errors);
        }

        // GET: api/Users (Admin only)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        // PUT: api/Users/{id}/role (Admin only)
        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserRole(string id, [FromQuery] bool isAdmin)
        {
            var result = await _userService.UpdateUserRoleAsync(id, isAdmin);
            if (result.Succeeded)
            {
                return Ok(new { Message = $"User role updated successfully. IsAdmin: {isAdmin}" });
            }
            return BadRequest(result.Errors);
        }
    }
}