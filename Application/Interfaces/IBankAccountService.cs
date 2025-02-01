using Application.DTOs;

namespace Application.Interfaces
{
    public interface IBankAccountService
    {
        Task<BankAccountResponseDto> CreateBankAccountAsync(BankAccountCreateDto createDto);
        Task<BankAccountResponseDto> UpdateBankAccountAsync(Guid id, BankAccountUpdateDto updateDto);
        Task<BankAccountResponseDto> GetBankAccountAsync(Guid id);
        Task<IEnumerable<BankAccountResponseDto>> GetSellerBankAccountsAsync(Guid sellerId);
        Task<bool> DeleteBankAccountAsync(Guid id, Guid sellerId);
        Task<BankAccountValidationDto> ValidateBankAccountAsync(BankAccountCreateDto createDto);
        Task<bool> VerifyBankAccountAsync(Guid id);
    }
}
