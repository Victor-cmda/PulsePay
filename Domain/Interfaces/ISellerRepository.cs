namespace Domain.Interfaces
{
    public interface ISellerRepository
    {
        Task<IEnumerable<Seller>> GetSellers(int Id);
        
    }
}
