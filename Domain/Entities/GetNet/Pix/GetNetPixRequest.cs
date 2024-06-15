namespace Domain.Entities.GetNet.Pix
{
    public class GetNetPixRequest
    {
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string order_id { get; set; }
        public string customer_id { get; set; }
    }
}
