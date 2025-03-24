namespace Shared.Enums
{
    public enum WithdrawStatus
    {
        Pending,    // Aguardando aprovação do admin
        Approved,   // Aprovado pelo admin, aguardando processamento
        Processing, // Em processamento
        Completed,  // Concluído com sucesso
        Failed,     // Falhou
        Rejected    // Rejeitado pelo admin
    }

}
