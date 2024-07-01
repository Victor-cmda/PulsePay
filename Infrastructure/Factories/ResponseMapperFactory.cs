using Application.Interfaces;
using Application.Mappers;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Factories
{
    public class ResponseMapperFactory : IResponseMapperFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ResponseMapperFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IResponseMapper<TResponse, TDto> CreateMapper<TResponse, TDto>()
        {
            return _serviceProvider.GetRequiredService<IResponseMapper<TResponse, TDto>>();
        }
    }
}
