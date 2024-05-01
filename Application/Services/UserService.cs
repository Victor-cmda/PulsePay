using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task RegisterUserAsync(UserRegistrationDto userDto)
        {
            var email = new Email(userDto.Email);
            var user = new User(email, userDto.Password);
            await _userRepository.AddUserAsync(user);
        }
    }
}
