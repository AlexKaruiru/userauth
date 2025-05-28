using userauth.DTOs;
using userauth.Models;

namespace userauth.Interfaces
{
    public interface IUserService
    {
        Task<User> Register(RegisterDto registerDto);
        Task<string> Login(LoginDto loginDto);
        Task<UserProfileDto> GetUserProfile(int userId);
        Task<UserProfileDto> UpdateUserProfile(int userId, UserUpdateDto updateDto);
        Task<UserProfileDto> AdminUpdateUser(int adminId, int userId, AdminUserUpdateDto updateDto);
        Task<List<UserProfileDto>> GetAllUsers(int adminId);
        Task<UserProfileDto> GetUserById(int requestingUserId, int userId);
        Task<bool> DeleteUser(int requestingUserId, int userId);
    }
}