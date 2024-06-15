using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Factories
{
    public class AuthenticationFactory : IAuthenticationFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public AuthenticationFactory(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public IAuthenticationPaymentApiService CreateAuthentication(string requestType)
        {
            var gatewayType = GetPaymentTypeRequest(requestType);
            switch (gatewayType)
            {
                case "GetNet":
                    return _serviceProvider.GetRequiredService<GetNetAuthenticationService>();
                case "K8Pay":
                    return _serviceProvider.GetRequiredService<GetNetAuthenticationService>();
                default:
                    throw new ArgumentException("Unsupported service type", nameof(gatewayType));
            }
        }

        private string GetPaymentTypeRequest(string requestType)
        {
            string result;
            switch (requestType)
            {
                case "Pix":
                    result = _configuration["PaymentService:Pix:GatewayType"];
                    break;
                case "BankSlip":
                    result = _configuration["PaymentService:BankSlip:GatewayType"];
                    break;
                default:
                    throw new ArgumentException("Unsupported service type", nameof(requestType));
            }
            return result;
        }
    }
}
