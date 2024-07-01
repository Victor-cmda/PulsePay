using Application.Mappers;

namespace Application.Interfaces
{
    public interface IResponseMapperFactory
    {
        IResponseMapper<TResponse, TDto> CreateMapper<TResponse, TDto>();
    }
}
