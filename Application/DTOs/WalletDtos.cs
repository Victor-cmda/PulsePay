using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    /// <summary>
    /// DTO para criação de carteira
    /// </summary>
    public class WalletCreateDto
    {
        [Required(ErrorMessage = "O ID do vendedor é obrigatório")]
        public Guid SellerId { get; set; }
    }

    /// <summary>
    /// DTO para atualização de saldo da carteira
    /// </summary>
    public class WalletUpdateDto
    {
        [Required(ErrorMessage = "O saldo disponível é obrigatório")]
        [Range(0, double.MaxValue, ErrorMessage = "O saldo disponível deve ser um valor não negativo")]
        public decimal AvailableBalance { get; set; }

        [Required(ErrorMessage = "O saldo pendente é obrigatório")]
        [Range(0, double.MaxValue, ErrorMessage = "O saldo pendente deve ser um valor não negativo")]
        public decimal PendingBalance { get; set; }
    }

    /// <summary>
    /// DTO para lidar com operações de fundos (depósitos e saques)
    /// </summary>
    public class WalletOperationDto
    {
        [Required(ErrorMessage = "O valor da operação é obrigatório")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Amount { get; set; }

        [StringLength(500, ErrorMessage = "A descrição não pode exceder 500 caracteres")]
        public string Description { get; set; }

        [StringLength(100, ErrorMessage = "A referência não pode exceder 100 caracteres")]
        public string Reference { get; set; }
    }

    /// <summary>
    /// DTO para resposta com detalhes da carteira
    /// </summary>
    public class WalletDto
    {
        public Guid Id { get; set; }
        public Guid SellerId { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal TotalBalance { get; set; }
        public DateTime LastUpdateAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO para transações da carteira
    /// </summary>
    public class WalletTransactionDto
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public string Reference { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    public class WalletBalanceDto
    {
        public Guid WalletId { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal TotalBalance { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// DTO para resposta com carteira e suas transações recentes
    /// </summary>
    public class WalletWithTransactionsDto
    {
        public WalletDto Wallet { get; set; }
        public List<WalletTransactionDto> RecentTransactions { get; set; }
    }
}