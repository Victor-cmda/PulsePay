using Application.DTOs;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.API
{
    [ApiController]
    [Route("api/getnet")]
    public class GetNetListenerController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public GetNetListenerController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet("pix")]
        public async Task<IActionResult> PixNotification([FromQuery] string payment_type,
            [FromQuery] string customer_id,
            [FromQuery] string order_id,
            [FromQuery] string payment_id,
            [FromQuery] int amount,
            [FromQuery] string status,
            [FromQuery] string transaction_id,
            [FromQuery] string transaction_timestamp,
            [FromQuery] string receiver_psp_name,
            [FromQuery] string receiver_psp_code,
            [FromQuery] string receiver_name,
            [FromQuery] string receiver_cnpj,
            [FromQuery] string receiver_cpf,
            [FromQuery] string terminal_nsu)
        {
            Console.WriteLine($"Notificação de PIX Recebida: {payment_type}, {customer_id}, {order_id}, {payment_id}, {amount}, {status}, {transaction_id}, {transaction_timestamp}, {receiver_psp_name}, {receiver_psp_code}, {receiver_name}, {receiver_cnpj}, {receiver_cpf}, {terminal_nsu}");

            return Ok();
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
