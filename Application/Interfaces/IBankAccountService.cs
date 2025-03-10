using Application.DTOs;

namespace Application.Interfaces
{
    public interface IBankAccountService
    {
        Task<BankAccountResponseDto> GetBankAccountAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<BankAccountResponseDto>> GetSellerBankAccountsAsync(Guid sellerId, CancellationToken cancellationToken = default);
        Task<BankAccountResponseDto> CreateBankAccountAsync(BankAccountCreateDto createDto, CancellationToken cancellationToken = default);
        Task<BankAccountResponseDto> UpdateBankAccountAsync(Guid id, BankAccountUpdateDto updateDto, CancellationToken cancellationToken = default);
        Task<bool> DeleteBankAccountAsync(Guid id, Guid sellerId, CancellationToken cancellationToken = default);
        Task<BankAccountValidationDto> ValidateBankAccountAsync(BankAccountCreateDto createDto, CancellationToken cancellationToken = default);
        Task<bool> VerifyBankAccountAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<BankAccountResponseDto>> GetUnverifiedAccountsAsync(int page = 1, int pageSize = 20);
        Task<int> GetTotalAccountsCountAsync();
        Task<bool> RejectBankAccountAsync(Guid id, string reason, CancellationToken cancellationToken = default);
    }
}