using userauth.DTOs;
using Microsoft.AspNetCore.Identity;

namespace userauth.Interfaces
{
    public interface IUserService
    {
        Task<IdentityResult> RegisterUserAsync(UserRegisterDto registerDto);
        Task<string> LoginUserAsync(UserLoginDto loginDto); // Returns JWT token
        Task<UserProfileDto> GetUserProfileAsync(string userId);
        Task<IdentityResult> UpdateUserProfileAsync(string userId, UserUpdateProfileDto updateDto);
        Task<IdentityResult> DeleteUserAsync(string userId);
        Task<IEnumerable<UserProfileDto>> GetAllUsersAsync(); // For admin purposes
        Task<IdentityResult> UpdateUserRoleAsync(string userId, bool isAdmin); // For admin purposes
    }
}