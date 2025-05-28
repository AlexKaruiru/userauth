using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using userauth.Data;
using userauth.DTOs;
using userauth.Interfaces;
using userauth.Models;

namespace userauth.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UserService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<User> Register(RegisterDto registerDto)
        {
            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            {
                throw new Exception("Email already exists");
            }

            CreatePasswordHash(registerDto.Password, out byte[] passwordHash, out byte[] passwordSalt);

            var user = new User
            {
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Phone = registerDto.Phone,
                Username = GenerateUsername(registerDto.FirstName, registerDto.LastName),
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Role = "customer",
                IsAdmin = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<string> Login(LoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            if (!VerifyPasswordHash(loginDto.Password, user.PasswordHash, user.PasswordSalt))
            {
                throw new Exception("Wrong password");
            }

            return CreateToken(user);
        }

        public async Task<UserProfileDto> GetUserProfile(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            return new UserProfileDto
            {
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                Role = user.Role
            };
        }

        public async Task<UserProfileDto> UpdateUserProfile(int userId, UserUpdateDto updateDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            if (!string.IsNullOrEmpty(updateDto.FirstName))
            {
                user.FirstName = updateDto.FirstName;
                user.Username = GenerateUsername(updateDto.FirstName, user.LastName);
            }

            if (!string.IsNullOrEmpty(updateDto.LastName))
            {
                user.LastName = updateDto.LastName;
                user.Username = GenerateUsername(user.FirstName, updateDto.LastName);
            }

            if (!string.IsNullOrEmpty(updateDto.Phone))
            {
                user.Phone = updateDto.Phone;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetUserProfile(userId);
        }

        public async Task<UserProfileDto> AdminUpdateUser(int adminId, int userId, AdminUserUpdateDto updateDto)
        {
            var admin = await _context.Users.FindAsync(adminId);
            if (admin == null || !admin.IsAdmin)
            {
                throw new Exception("Unauthorized");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            if (!string.IsNullOrEmpty(updateDto.FirstName))
            {
                user.FirstName = updateDto.FirstName;
                user.Username = GenerateUsername(updateDto.FirstName, user.LastName);
            }

            if (!string.IsNullOrEmpty(updateDto.LastName))
            {
                user.LastName = updateDto.LastName;
                user.Username = GenerateUsername(user.FirstName, updateDto.LastName);
            }

            if (!string.IsNullOrEmpty(updateDto.Phone))
            {
                user.Phone = updateDto.Phone;
            }

            if (!string.IsNullOrEmpty(updateDto.Role))
            {
                user.Role = updateDto.Role;
            }

            if (updateDto.IsAdmin.HasValue)
            {
                user.IsAdmin = updateDto.IsAdmin.Value;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetUserProfile(userId);
        }

        public async Task<List<UserProfileDto>> GetAllUsers(int adminId)
        {
            var admin = await _context.Users.FindAsync(adminId);
            if (admin == null || !admin.IsAdmin)
            {
                throw new Exception("Unauthorized");
            }

            return await _context.Users
                .Select(u => new UserProfileDto
                {
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Phone = u.Phone,
                    Role = u.Role
                })
                .ToListAsync();
        }

        // Add these two methods to your existing service
        public async Task<UserProfileDto> GetUserById(int requestingUserId, int userId)
        {
            var requestingUser = await _context.Users.FindAsync(requestingUserId);
            if (requestingUser == null)
            {
                throw new Exception("Requesting user not found");
            }

            // Only allow if requesting user is admin or is requesting their own profile
            if (!requestingUser.IsAdmin && requestingUserId != userId)
            {
                throw new Exception("Unauthorized");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            return new UserProfileDto
            {
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                Role = user.Role
            };
        }

        public async Task<bool> DeleteUser(int requestingUserId, int userId)
        {
            var requestingUser = await _context.Users.FindAsync(requestingUserId);
            if (requestingUser == null || !requestingUser.IsAdmin)
            {
                throw new Exception("Unauthorized - Admin access required");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using var hmac = new HMACSHA512();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using var hmac = new HMACSHA512(passwordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(passwordHash);
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration.GetSection("Jwt:Key").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateUsername(string firstName, string lastName)
        {
            var baseUsername = $"{firstName.ToLower()}{lastName.ToLower()}";
            var username = baseUsername;
            var counter = 1;

            while (_context.Users.Any(u => u.Username == username))
            {
                username = $"{baseUsername}{counter}";
                counter++;
            }

            return username;
        }
    }
}