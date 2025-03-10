namespace Shared.Enums
{
    public enum ClientPaymentStatus
    {
        Pending,    // Solicitação recebida, aguardando aprovação do admin
        Approved,   // Aprovado pelo admin, aguardando processamento
        Processing, // Em processamento
        Completed,  // Concluído com sucesso
        Failed,     // Falhou
        Rejected    // Rejeitado pelo admin
    }
}
