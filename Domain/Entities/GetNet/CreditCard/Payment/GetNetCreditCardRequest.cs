namespace Domain.Entities.GetNet.CreditCard.Payment
{
    public class GetNetCreditCardRequest
    {
        public string seller_id { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public Order order { get; set; }
        public Customer customer_credit { get; set; }
        public Device device { get; set; }
        public List<Shipping> shippings { get; set; }
        public SubMerchant sub_merchant { get; set; }
        public CreditRequest credit { get; set; }
        public Tokenization tokenization { get; set; }
        public Wallet wallet { get; set; }
    }

    public class Order
    {
        public string order_id { get; set; }
        public decimal sales_tax { get; set; }
        public string product_type { get; set; }
    }

    public class Customer
    {
        public string customer_id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string document_type { get; set; }
        public string document_number { get; set; }
        public string phone_number { get; set; }
        public BillingAddress billing_address { get; set; }
    }

    public class BillingAddress
    {
        public string street { get; set; }
        public string number { get; set; }
        public string complement { get; set; }
        public string district { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string postal_code { get; set; }
    }

    public class Device
    {
        public string ip_address { get; set; }
        public string device_id { get; set; }
    }

    public class Shipping
    {
        public string first_name { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string phone_number { get; set; }
        public decimal shipping_amount { get; set; }
        public Address address { get; set; }
    }

    public class Address
    {
        public string street { get; set; }
        public string number { get; set; }
        public string complement { get; set; }
        public string district { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string postal_code { get; set; }
    }

    public class SubMerchant
    {
        public string identification_code { get; set; }
        public string document_type { get; set; }
        public string document_number { get; set; }
        public string address { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string postal_code { get; set; }
    }

    public class CreditRequest
    {
        public bool delayed { get; set; }
        public bool pre_authorization { get; set; }
        public bool save_card_data { get; set; }
        public string transaction_type { get; set; }
        public int number_installments { get; set; }
        public string soft_descriptor { get; set; }
        public int dynamic_mcc { get; set; }
        public Card card { get; set; }
        public string credentials_on_file_type { get; set; }
        public long transaction_id { get; set; }
    }

    public class Card
    {
        public string number_token { get; set; }
        public string cardholder_name { get; set; }
        public string security_code { get; set; }
        public string brand { get; set; }
        public string expiration_month { get; set; }
        public string expiration_year { get; set; }
    }

    public class Tokenization
    {
        public string type { get; set; }
        public string cryptogram { get; set; }
        public string eci { get; set; }
        public List<object> RequestorId { get; set; }
    }

    public class Wallet
    {
        public string type { get; set; }
        public string id { get; set; }
        public string merchant_id { get; set; }
        public FundTransfer fund_transfer { get; set; }
    }

    public class FundTransfer
    {
        public string pay_action { get; set; }
        public Receiver Receiver { get; set; }
    }

    public class Receiver
    {
        public string account_number { get; set; }
        public string account_type { get; set; }
        public string first_name { get; set; }
        public string middle_name { get; set; }
        public string last_name { get; set; }
        public string addr_street { get; set; }
        public string addr_city { get; set; }
        public string addr_state { get; set; }
        public string addr_country { get; set; }
        public string addr_postal_code { get; set; }
        public string nationality { get; set; }
        public string phone { get; set; }
        public string date_of_birth { get; set; }
        public string id_type { get; set; }
        public string id_num { get; set; }
    }
}
