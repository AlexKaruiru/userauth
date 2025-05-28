using AutoMapper;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text;
using userauth.DTOs; 
using userauth.Interfaces; 
using userauth.Models; 

namespace userauth.Services 
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public UserService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IConfiguration configuration,
            IMapper mapper)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<IdentityResult> RegisterUserAsync(UserRegisterDto registerDto)
        {
            var user = _mapper.Map<User>(registerDto);
            user.UserName = registerDto.Email; // Set UserName to Email for login consistency
            user.EmailConfirmed = true; // For simple registration, assume email is confirmed

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                // Assign a default role, e.g., "User"
                await _userManager.AddToRoleAsync(user, "User");
                if (user.IsAdmin)
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                }
            }
            return result;
        }

        public async Task<string> LoginUserAsync(UserLoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return null; // User not found
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded)
            {
                return null; // Invalid credentials
            }

            return await GenerateJwtToken(user);
        }

        public async Task<UserProfileDto> GetUserProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return null;
            }
            return _mapper.Map<UserProfileDto>(user);
        }

        public async Task<IdentityResult> UpdateUserProfileAsync(string userId, UserUpdateProfileDto updateDto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            // Update only the provided fields
            if (!string.IsNullOrEmpty(updateDto.FirstName))
            {
                user.FirstName = updateDto.FirstName;
            }
            if (!string.IsNullOrEmpty(updateDto.LastName))
            {
                user.LastName = updateDto.LastName;
            }
            if (!string.IsNullOrEmpty(updateDto.PhoneNumber))
            {
                user.PhoneNumber = updateDto.PhoneNumber;
            }

            user.UpdatedAt = DateTime.UtcNow; // Update timestamp

            return await _userManager.UpdateAsync(user);
        }

        public async Task<IdentityResult> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }
            return await _userManager.DeleteAsync(user);
        }

        public async Task<IEnumerable<UserProfileDto>> GetAllUsersAsync()
        {
            var users = _userManager.Users.ToList(); // Get all users
            return _mapper.Map<IEnumerable<UserProfileDto>>(users);
        }

        public async Task<IdentityResult> UpdateUserRoleAsync(string userId, bool isAdmin)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            if (isAdmin && !currentRoles.Contains("Admin"))
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }
            else if (!isAdmin && currentRoles.Contains("Admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
            }

            user.IsAdmin = isAdmin; // Update custom IsAdmin property
            user.UpdatedAt = DateTime.UtcNow;

            return await _userManager.UpdateAsync(user);
        }


        private async Task<string> GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id), // Standard claim for user ID
                new Claim(ClaimTypes.Name, user.Email), // Use email as the principal name
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName), // Custom claim for full name
                new Claim("IsAdmin", user.IsAdmin.ToString()) // Custom claim for IsAdmin
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpireMinutes"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}