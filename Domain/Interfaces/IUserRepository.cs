namespace Domain.Interfaces
{
    public interface IUserRepository
    {
        Task AddUserAsync(User user);
    }
}
