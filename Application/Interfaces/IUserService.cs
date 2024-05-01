using Application.DTOs;

namespace Application.Interfaces
{
    public interface IUserService
    {
        Task RegisterUserAsync(UserRegistrationDto userDto);
    }
}
