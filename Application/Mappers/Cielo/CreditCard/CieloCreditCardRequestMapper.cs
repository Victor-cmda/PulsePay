using Application.DTOs.CreditCard;
using Domain.Entities.Cielo.CreditCard;

namespace Application.Mappers.GetNet.CreditCard
{
    public class CieloCreditCardRequestMapper : IResponseMapper<PaymentCreditCardRequestDto, CieloCreditCardRequest>
    {
        public CieloCreditCardRequest Map(PaymentCreditCardRequestDto response)
        {
            return new CieloCreditCardRequest
            {
                MerchantOrderId = "2014111903",
                Payment = new PaymentRequest
                {
                    Type = "CreditCard",
                    Amount = response.Amount,
                    Provider = "Cielo",
                    Installments = 1,
                    SoftDescriptor = "PULSEPAY*PAGAMENTOS",
                    Recurrent = false,
                    Currency = "BRL",
                    CreditCard = new CreditCardRequest
                    {
                        CardNumber = response.Card.CardNumber,
                        Holder = response.Card.CardHolderName,
                        SaveCard = false,
                        Brand = response.Card.CardBrand,
                        ExpirationDate = $"{response.Card.ExpirationMonth}/{response.Card.ExpirationYear}",
                        SecurityCode = response.Card.SecurityCode,
                    },
                    InitiatedTransactionIndicator = new InitiatedTransactionIndicatorRequest
                    {
                        Category = "C1",
                        Subcategory = "Installment"
                    }
                },
                Customer = new CustomerRequest
                {
                    Name = response.Customer.Name,
                    Email = response.Customer.Email,
                    Address = new AddressRequest
                    {
                        City = response.Customer.BillingAddress.City,
                        State = response.Customer.BillingAddress.State,
                        Country = response.Customer.BillingAddress.Country,
                        Street = response.Customer.BillingAddress.Street,
                        Complement = response.Customer.BillingAddress.Complement,
                        Number = response.Customer.BillingAddress.Number,
                        ZipCode = response.Customer.BillingAddress.PostalCode
                    }
                }
            };
        }
    }
}


