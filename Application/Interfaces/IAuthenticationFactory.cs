namespace Application.Interfaces
{
    public interface IAuthenticationFactory
    {
        IAuthenticationPaymentApiService CreateAuthentication(string requestType);
    }
}
