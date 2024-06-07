using Application.Interfaces;
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
        public IPaymentGateway CreateGateway()
        {
            var gatewayType = _configuration["PaymentService:GatewayType"];
            switch (gatewayType)
            {
                case "GetNet":
                    return new GetNetAdapter(new HttpClient(), _configuration);
                case "PixFast":
                    return new GetNetAdapter(new HttpClient(), _configuration);
                default:
                    throw new ArgumentException("Unsupported gateway type");
            }
        }
    }

}
