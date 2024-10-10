using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.API
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/getnet")]
    public class GetNetListenerController : ControllerBase
    {
        private readonly IListenerService _listenerService;

        public GetNetListenerController(IListenerService listenerService)
        {
            _listenerService = listenerService;
        }

        [HttpGet("pix")]
        public async Task<IActionResult> ReceivePaymentNotification([FromQuery] Dictionary<string, string> queryParams)
        {
            try
            {
                string paymentType = queryParams.ContainsKey("payment_type") ? queryParams["payment_type"] : null;
                string status = queryParams.ContainsKey("status") ? queryParams["status"] : null;
                string orderId = queryParams.ContainsKey("order_id") ? queryParams["order_id"] : null;
                string transactionId = queryParams.ContainsKey("transaction_id") ? queryParams["transaction_id"] : null;
                string customerId = queryParams.ContainsKey("customer_id") ? queryParams["customer_id"] : null;
                string paymentId = queryParams.ContainsKey("payment_id") ? queryParams["payment_id"] : null;
                string transactionTimestampStr = queryParams.ContainsKey("transaction_timestamp") ? queryParams["transaction_timestamp"] : null;

                DateTime transactionTimestamp;
                if (!DateTime.TryParse(transactionTimestampStr, out transactionTimestamp))
                {
                    return BadRequest("Invalid transaction_timestamp format.");
                }

                await _listenerService.GenerateNotification(new NotificationDto
                {
                    OrderId = orderId,
                    Description = $"{status} AT {transactionTimestamp}",
                    Status = status,
                    TransactionId = transactionId,
                    CustomerId = customerId,
                    PaymentId = paymentId,
                    PaymentType = paymentType,
                    TransactionTimestamp = transactionTimestamp,
                });

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("debit")]
        public async Task<IActionResult> DebitPaymentNotify([FromQuery] string payment_type,
            [FromQuery] string customer_id,
            [FromQuery] string order_id,
            [FromQuery] string payment_id,
            [FromQuery] int amount,
            [FromQuery] string status,
            [FromQuery] string acquirer_transaction_id,
            [FromQuery] string authorization_timestamp,
            [FromQuery] string brand,
            [FromQuery] string terminal_nsu,
            [FromQuery] string authorization_code)
        {
            Console.WriteLine($"Notificação de Débito Recebida: {payment_type}, {customer_id}, {order_id}, {payment_id}, {amount}, {status}, {acquirer_transaction_id}, {authorization_timestamp}, {brand}, {terminal_nsu}, {authorization_code}");

            return Ok();
        }

        [HttpGet("credit")]
        public async Task<IActionResult> CreditNotification([FromQuery] string payment_type,
            [FromQuery] string customer_id,
            [FromQuery] string order_id,
            [FromQuery] string payment_id,
            [FromQuery] int amount,
            [FromQuery] string status,
            [FromQuery] int number_installments,
            [FromQuery] string acquirer_transaction_id,
            [FromQuery] string authorization_timestamp,
            [FromQuery] string brand,
            [FromQuery] string terminal_nsu,
            [FromQuery] string authorization_code,
            [FromQuery] string combined_id)
        {
            Console.WriteLine($"Notificação de Crédito Recebida: {payment_type}, {customer_id}, {order_id}, {payment_id}, {amount}, {status}, {number_installments}, {acquirer_transaction_id}, {authorization_timestamp}, {brand}, {terminal_nsu}, {authorization_code}, {combined_id}");

            return Ok();
        }

        [HttpGet("bank-slip")]
        public async Task<IActionResult> BankSlipNotification([FromQuery] string payment_type,
            [FromQuery] string order_id,
            [FromQuery] string payment_id,
            [FromQuery] string id,
            [FromQuery] int amount,
            [FromQuery] string status,
            [FromQuery] string bank,
            [FromQuery] string our_number,
            [FromQuery] string typeful_line,
            [FromQuery] string issue_date,
            [FromQuery] string expiration_date,
            [FromQuery] string error_code,
            [FromQuery] string description_detail)
        {
            Console.WriteLine($"Notificação de Boleto Recebida: {payment_type}, {order_id}, {payment_id}, {id}, {amount}, {status}, {bank}, {our_number}, {typeful_line}, {issue_date}, {expiration_date}, {error_code}, {description_detail}");

            return Ok();
        }

        [HttpGet("recurrency")]
        public async Task<IActionResult> RecurrencyNotification([FromQuery] string payment_type,
            [FromQuery] string order_id,
            [FromQuery] string payment_id,
            [FromQuery] int amount,
            [FromQuery] string status,
            [FromQuery] string authorization_timestamp,
            [FromQuery] string acquirer_transaction_id,
            [FromQuery] string customer_id,
            [FromQuery] string subscription_id,
            [FromQuery] string plan_id,
            [FromQuery] string charge_id,
            [FromQuery] int number_installments,
            [FromQuery] int billing_number,
            [FromQuery] string brand,
            [FromQuery] string terminal_nsu,
            [FromQuery] string authorization_code,
            [FromQuery] int retry_number,
            [FromQuery] string error_code,
            [FromQuery] string description_detail)
        {
            Console.WriteLine($"Notificação de Recorrência Recebida: {payment_type}, {order_id}, {payment_id}, {amount}, {status}, {authorization_timestamp}, {acquirer_transaction_id}, {customer_id}, {subscription_id}, {plan_id}, {charge_id}, {number_installments}, {billing_number}, {brand}, {terminal_nsu}, {authorization_code}, {retry_number}, {error_code}, {description_detail}");

            return Ok();
        }
    }
}
