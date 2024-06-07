namespace Application.Interfaces
{
    public interface IPaymentGatewayFactory
    {
        IPaymentGateway CreateGateway();
    }
}
