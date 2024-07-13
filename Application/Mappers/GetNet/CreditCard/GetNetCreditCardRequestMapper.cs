using Application.DTOs.CreditCard;
using Domain.Entities.GetNet.CreditCard.Payment;

namespace Application.Mappers.GetNet.CreditCard
{
    public class GetNetCreditCardRequestMapper : IResponseMapper<PaymentCreditCardRequestDto, GetNetCreditCardRequest>
    {
        public GetNetCreditCardRequest Map(PaymentCreditCardRequestDto response)
        {
            return new GetNetCreditCardRequest
            {
                amount = response.Amount,
                currency = "BRL",
                order = new Domain.Entities.GetNet.CreditCard.Payment.Order
                {
                    order_id = response.Order.Id,
                    product_type = "service",
                    sales_tax = 0
                },
                customer = new Domain.Entities.GetNet.CreditCard.Payment.Customer
                {
                    customer_id = response.Customer.Id,
                    first_name = response.Customer.FirstName,
                    last_name = response.Customer.LastName,
                    name = response.Customer.Name,
                    email = response.Customer.Email,
                    document_type = response.Customer.DocumentType,
                    document_number = response.Customer.Document,
                    phone_number = response.Customer.PhoneNumber,
                    billing_address = new Domain.Entities.GetNet.CreditCard.Payment.BillingAddress
                    {
                        street = response.Customer.BillingAddress.Street,
                        number = response.Customer.BillingAddress.Number,
                        complement = response.Customer.BillingAddress.Complement,
                        district = response.Customer.BillingAddress.District,
                        city = response.Customer.BillingAddress.City,
                        state = response.Customer.BillingAddress.State,
                        country = response.Customer.BillingAddress.Country,
                        postal_code = response.Customer.BillingAddress.PostalCode
                    }
                },
                credit = new CreditRequest
                {
                    card = new Domain.Entities.GetNet.CreditCard.Payment.Card
                    {
                        cardholder_name = response.Card.CardHolderName,
                        security_code = response.Card.SecurityCode,
                        brand = response.Card.CardBrand,
                        expiration_month = response.Card.ExpirationMonth,
                        expiration_year = response.Card.ExpirationYear,
                    },
                    delayed = false,
                    pre_authorization = false,
                    save_card_data = false,
                    transaction_type = "FULL",
                    number_installments = 1,
                    soft_descriptor = "PULSEPAY*PAGAMENTOS",
                    credentials_on_file_type = "ONE_CLICK_PAYMENT",
                    transaction_id = 1002217281190421,
                    dynamic_mcc = 6531
                },
                wallet = new Wallet
                {
                    type = "55",
                    id = "000",
                    fund_transfer = new FundTransfer
                    {
                        pay_action = "FT",
                    }
                },
                sub_merchant = new SubMerchant
                {
                    identification_code = "9058344",
                    document_type = "CNPJ",
                    document_number = "06729003000147",
                    address = "RUA CRAVINA, 413 SALA01 - VILA ADRIANA",
                    city = "Foz do Iguacu",
                    postal_code = "85854495",
                    state = "PR"
                },
            };
        }
    }
}


