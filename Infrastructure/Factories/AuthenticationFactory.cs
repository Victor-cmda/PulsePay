using Application.Interfaces;
using Application.Interfaces.Application.Interfaces;
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

        public IAuthenticationPaymentApiService CreateAuthentication(string serviceType)
        {
            switch (serviceType)
            {
                case "GetNet":
                    return _serviceProvider.GetRequiredService<GetNetAuthenticationService>();
                case "Sicredi":
                    return _serviceProvider.GetRequiredService<GetNetAuthenticationService>();
                default:
                    throw new ArgumentException("Unsupported service type", nameof(serviceType));
            }
        }
    }
}
