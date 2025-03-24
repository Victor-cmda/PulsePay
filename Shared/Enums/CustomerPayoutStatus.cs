namespace Shared.Enums
{
    public enum CustomerPayoutStatus
    {
        Pending,       // Solicitação inicial
        Validated,     // Chave PIX validada
        InProgress,    // Em processamento pelo admin
        Completed,     // Pagamento concluído
        Rejected,      // Rejeitado pelo admin
        Failed         // Falha no processamento
    }
}
