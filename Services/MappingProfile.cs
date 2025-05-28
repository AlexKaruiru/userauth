using AutoMapper;
using userauth.DTOs;
using userauth.Models;

namespace userauth.Services
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<UserRegisterDto, User>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email)); // Map email to UserName for Identity
            CreateMap<User, UserProfileDto>();
            CreateMap<UserUpdateProfileDto, User>(); // For updating user properties
        }
    }
}