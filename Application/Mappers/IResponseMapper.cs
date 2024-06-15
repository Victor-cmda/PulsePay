namespace Application.Mappers
{
    public interface IResponseMapper<in T, out U>
    {
        U Map(T response);
    }
}
