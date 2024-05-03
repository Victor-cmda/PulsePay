using Domain.Interfaces;

namespace Application.Interfaces
{
    namespace Application.Interfaces
    {
        public interface IPaymentGatewayFactory
        {
            IPaymentGateway CreateGateway(string type);
        }
    }
}
