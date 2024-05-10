using Domain.Interfaces;

namespace Application.Interfaces
{
    namespace Application.Interfaces
    {
        public interface IAuthenticationFactory
        {
            IAuthenticationPaymentApiService CreateAuthentication(string type);
        }
    }
}
