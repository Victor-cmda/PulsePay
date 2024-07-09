using Application.DTOs.CreditCard.Payment;
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
                customer_credit = new Domain.Entities.GetNet.CreditCard.Payment.Customer
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
                        security_code = response.Card.SecurityCode.ToString(),
                        brand = response.Card.CardBrand,
                        expiration_month = response.Card.ExpirationMonth,
                        expiration_year = response.Card.ExpirationYear,

                    },
                    delayed = false,
                    pre_authorization = false,
                    save_card_data = false,
                    transaction_type = "FULL",
                    number_installments = 1,
                    soft_descriptor = "PulsePay Pagamentos"
                },
                shippings = new List<Shipping>()
                {
                    new Shipping
                    {
                        first_name = response.Customer.FirstName,
                        name = response.Customer.Name,
                        email = response.Customer.Email,
                        shipping_amount = 0,
                        phone_number = response.Customer.PhoneNumber,
                        address = new Address
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
                    }
                },
            };
        }
    }
}


