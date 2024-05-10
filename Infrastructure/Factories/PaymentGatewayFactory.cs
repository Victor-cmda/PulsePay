using Application.Interfaces;
using Application.Interfaces.Application.Interfaces;
using Infrastructure.Adapters.PaymentGateway;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Factories
{
    public class PaymentGatewayFactory : IPaymentGatewayFactory
    {
        private readonly IConfiguration _configuration;
        public PaymentGatewayFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public IPaymentGateway CreateGateway(string gatewayType)
        {
            switch (gatewayType)
            {
                case "GetNet":
                    return new GetNetAdapter(new HttpClient(), "https://api-sandbox.getnet.com.br/v1/", _configuration);
                case "PixFast":
                    return new GetNetAdapter(new HttpClient(), "https://api.pixfast.com.br", _configuration);
                default:
                    throw new ArgumentException("Unsupported gateway type");
            }
        }
    }

}
