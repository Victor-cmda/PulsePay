namespace Application.Interfaces
{
    public interface IAuthenticationPaymentApiService
    {
        Task<string> GetTokenAsync();
    }
}
