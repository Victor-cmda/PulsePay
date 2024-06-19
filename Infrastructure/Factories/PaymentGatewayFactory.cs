using Application.Interfaces;
using Infrastructure.Adapters.PaymentGateway;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Factories
{
    public class PaymentGatewayFactory : IPaymentGatewayFactory
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        public PaymentGatewayFactory(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }
        public IPaymentGateway CreateGateway(string requestType)
        {
            var gatewayType = GetPaymentTypeRequest(requestType);
            switch (gatewayType)
            {
                case "GetNet":
                    return _serviceProvider.GetRequiredService<GetNetAdapter>();
                case "K8Pay":
                    return _serviceProvider.GetRequiredService<K8PayAdapter>();
                default:
                    throw new ArgumentException("Unsupported gateway type");
            }
        }

        private string GetPaymentTypeRequest(string requestType)
        {
            return requestType switch
            {
                "Pix" => _configuration["PaymentService:Pix:GatewayType"],
                "BankSlip" => _configuration["PaymentService:BankSlip:GatewayType"],
                _ => throw new ArgumentException("Unsupported service type", nameof(requestType))
            };
        }
    }

}
