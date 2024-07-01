using Application.DTOs.BankSlip;
using Domain.Entities.K8Pay.BankSlip;

namespace Application.Mappers.K8Pay
{
    public class K8PayBankSlipRequestMapper : IResponseMapper<PaymentBankSlipRequestDto, K8PayBankSlipRequest>
    {
        public K8PayBankSlipRequest Map(PaymentBankSlipRequestDto response)
        {
            return new K8PayBankSlipRequest
            {
                Valor = response.Amount,
                ClienteCPFCNPJ = response.Customer.DocumentNumber,
                ClienteNome = response.Customer.Name,
                ClienteCEP = response.Customer.BillingAddress.PostalCode,
                ClienteBairro= response.Customer.BillingAddress.District,
                ClienteCidade = response.Customer.BillingAddress.City,
                ClienteEndereco = $"{response.Customer.BillingAddress.Street}, {response.Customer.BillingAddress.Number}",
                ClienteUF = response.Customer.BillingAddress.State,
                ClienteEmail = response.Customer.Email,
                ClienteDescricao =  $"{response.Customer.Name} - {response.Customer.DocumentNumber}",
                ClienteIP = "127.0.0.1",
                DataVencimento = DateTime.Now.AddDays(7),
                URLConfirmacao = "https://localhost:5001/api/payment/confirm",
                RetornarBase64 = true,
                EntradaCNAB = false
            };
        }
    }
}
