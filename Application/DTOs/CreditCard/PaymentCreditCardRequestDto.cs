﻿namespace Application.DTOs.CreditCard
{
    public class PaymentCreditCardRequestDto
    {
        public int Amount { get; set; }
        public string OrderId { get; set; }
        public Card Card { get; set; }
        public Customer Customer { get; set; }
    }

    public class Card
    {
        public string CardNumber { get; set; }
        public string CardBrand { get; set; }
        public string ExpirationYear { get; set; }
        public string ExpirationMonth { get; set; }
        public string SecurityCode { get; set; }
        public string CardHolderName { get; set; }
    }

    public class Customer
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Name { get; set; }
        public string DocumentType { get; set; }
        public string Document { get; set; }
        public string PhoneNumber { get; set; }
        public string Birthdate { get; set; }
        public BillingAddress BillingAddress { get; set; }
    }

    public class BillingAddress
    {
        public string Street { get; set; }
        public string Number { get; set; }
        public string Complement { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
    }
}
