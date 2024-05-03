using Application.Interfaces;
using Application.Interfaces.Application.Interfaces;
using Domain.Interfaces;
using Infrastructure.Adapters.PaymentGateway;

namespace Infrastructure.Factories
{
    public class PaymentGatewayFactory : IPaymentGatewayFactory
    {
        public IPaymentGateway CreateGateway(string gatewayType)
        {
            switch (gatewayType)
            {
                case "GetNet":
                    return new GetNetAdapter(new HttpClient(), "https://api-homologacao.getnet.com.br/v1");
                case "PixFast":
                    return new GetNetAdapter(new HttpClient(), "https://api.pixfast.com.br");
                default:
                    throw new ArgumentException("Unsupported gateway type");
            }
        }
    }

}
