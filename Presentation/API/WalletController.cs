using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Common.Requests;
using Presentation.API.Common.Responses;
using Shared.Exceptions;
using System.Net;

namespace Presentation.API.Controllers
{
    [Authorize(Policy = "UserPolicy")]
    [ApiController]
    [Route("api/wallets")]
    [Produces("application/json")]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly ILogger<WalletController> _logger;

        public WalletController(
            IWalletService walletService,
            ILogger<WalletController> logger)
        {
            _walletService = walletService ?? throw new ArgumentNullException(nameof(walletService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtém a carteira de um vendedor pelo ID
        /// </summary>
        /// <param name="sellerId">ID do vendedor</param>
        /// <response code="200">Retorna a carteira solicitada</response>
        /// <response code="404">Carteira não encontrada</response>
        [HttpGet("{sellerId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWallet(Guid sellerId)
        {
            try
            {
                _logger.LogInformation("Obtendo carteira para o vendedor {SellerId}", sellerId);
                var wallet = await _walletService.GetWalletAsync(sellerId);
                return Ok(new ApiResponse<WalletDto>(wallet));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada para o vendedor {SellerId}", sellerId);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter carteira para o vendedor {SellerId}", sellerId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Obtém a carteira com transações recentes de um vendedor
        /// </summary>
        /// <param name="sellerId">ID do vendedor</param>
        /// <param name="count">Número de transações recentes a serem retornadas</param>
        /// <response code="200">Retorna a carteira com transações recentes</response>
        /// <response code="404">Carteira não encontrada</response>
        [HttpGet("{sellerId:guid}/with-transactions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWalletWithTransactions(Guid sellerId, [FromQuery] int count = 5)
        {
            try
            {
                _logger.LogInformation("Obtendo carteira com transações para o vendedor {SellerId}", sellerId);
                var walletWithTransactions = await _walletService.GetWalletWithRecentTransactionsAsync(sellerId, count);
                return Ok(new ApiResponse<WalletWithTransactionsDto>(walletWithTransactions));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada para o vendedor {SellerId}", sellerId);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter carteira com transações para o vendedor {SellerId}", sellerId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Cria uma nova carteira para um vendedor
        /// </summary>
        /// <param name="createDto">Dados para criação da carteira</param>
        /// <response code="201">Carteira criada com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="409">Carteira já existe</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateWallet([FromBody] WalletCreateDto createDto)
        {
            try
            {
                _logger.LogInformation("Criando carteira para o vendedor {SellerId}", createDto.SellerId);
                var wallet = await _walletService.CreateWalletAsync(createDto);
                return CreatedAtAction(nameof(GetWallet), new { sellerId = wallet.SellerId }, new ApiResponse<WalletDto>(wallet));
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, "Conflito ao criar carteira para o vendedor {SellerId}", createDto.SellerId);
                return Conflict(new ApiResponse<object>(HttpStatusCode.Conflict, ex.Message));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validação falhou ao criar carteira");
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar carteira para o vendedor {SellerId}", createDto.SellerId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Atualiza o saldo de uma carteira
        /// </summary>
        /// <param name="sellerId">ID do vendedor</param>
        /// <param name="updateDto">Dados para atualização do saldo</param>
        /// <response code="200">Saldo atualizado com sucesso</response>
        /// <response code="404">Carteira não encontrada</response>
        [HttpPut("{sellerId:guid}/balance")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateBalance(Guid sellerId, [FromBody] WalletUpdateDto updateDto)
        {
            try
            {
                _logger.LogInformation("Atualizando saldo para o vendedor {SellerId}", sellerId);
                var wallet = await _walletService.UpdateBalanceAsync(sellerId, updateDto);
                return Ok(new ApiResponse<WalletDto>(wallet));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada para o vendedor {SellerId}", sellerId);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validação falhou ao atualizar saldo");
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar saldo para o vendedor {SellerId}", sellerId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Adiciona fundos a uma carteira
        /// </summary>
        /// <param name="sellerId">ID do vendedor</param>
        /// <param name="request">Informações do depósito</param>
        /// <response code="200">Fundos adicionados com sucesso</response>
        /// <response code="400">Valor inválido</response>
        /// <response code="404">Carteira não encontrada</response>
        [HttpPost("{sellerId:guid}/deposits")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddFunds(Guid sellerId, [FromBody] FundsOperationRequest request)
        {
            try
            {
                _logger.LogInformation("Adicionando {Amount} à carteira do vendedor {SellerId}", request.Amount, sellerId);

                var operationDto = new WalletOperationDto
                {
                    Amount = request.Amount,
                    Description = request.Description ?? "Depósito de fundos",
                    Reference = request.Reference
                };

                var wallet = await _walletService.AddFundsAsync(sellerId, operationDto);
                return Ok(new ApiResponse<WalletDto>(wallet));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada para o vendedor {SellerId}", sellerId);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validação falhou ao adicionar fundos");
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar fundos para o vendedor {SellerId}", sellerId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Retira fundos de uma carteira
        /// </summary>
        /// <param name="sellerId">ID do vendedor</param>
        /// <param name="request">Informações da retirada</param>
        /// <response code="200">Fundos retirados com sucesso</response>
        /// <response code="400">Valor inválido ou saldo insuficiente</response>
        /// <response code="404">Carteira não encontrada</response>
        [HttpPost("{sellerId:guid}/withdrawals")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeductFunds(Guid sellerId, [FromBody] FundsOperationRequest request)
        {
            try
            {
                _logger.LogInformation("Retirando {Amount} da carteira do vendedor {SellerId}", request.Amount, sellerId);

                var operationDto = new WalletOperationDto
                {
                    Amount = request.Amount,
                    Description = request.Description ?? "Retirada de fundos",
                    Reference = request.Reference
                };

                var wallet = await _walletService.DeductFundsAsync(sellerId, operationDto);
                return Ok(new ApiResponse<WalletDto>(wallet));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada para o vendedor {SellerId}", sellerId);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validação falhou ao retirar fundos");
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (InsufficientFundsException ex)
            {
                _logger.LogWarning(ex, "Fundos insuficientes para o vendedor {SellerId}", sellerId);
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retirar fundos para o vendedor {SellerId}", sellerId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Obtém o histórico de transações de uma carteira
        /// </summary>
        /// <param name="sellerId">ID do vendedor</param>
        /// <param name="startDate">Data inicial</param>
        /// <param name="endDate">Data final</param>
        /// <param name="page">Página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <response code="200">Histórico obtido com sucesso</response>
        /// <response code="404">Carteira não encontrada</response>
        [HttpGet("{sellerId:guid}/transactions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTransactions(
            Guid sellerId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Obtendo transações para o vendedor {SellerId}", sellerId);
                var transactions = await _walletService.GetTransactionsAsync(sellerId, startDate, endDate, page, pageSize);
                return Ok(new ApiResponse<List<WalletTransactionDto>>(transactions));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada para o vendedor {SellerId}", sellerId);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter transações para o vendedor {SellerId}", sellerId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }
    }
}