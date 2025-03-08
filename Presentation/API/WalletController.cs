using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Common.Requests;
using Presentation.API.Common.Responses;
using Shared.Enums;
using Shared.Exceptions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

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
        /// Obtém uma carteira específica pelo ID
        /// </summary>
        /// <param name="id">ID da carteira</param>
        /// <response code="200">Retorna a carteira solicitada</response>
        /// <response code="404">Carteira não encontrada</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWallet(Guid id)
        {
            try
            {
                _logger.LogInformation("Obtendo carteira com ID {WalletId}", id);
                var wallet = await _walletService.GetWalletAsync(id);
                return Ok(new ApiResponse<WalletDto>(wallet));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada com ID {WalletId}", id);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter carteira com ID {WalletId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Obtém todas as carteiras de um vendedor
        /// </summary>
        /// <param name="sellerId">ID do vendedor</param>
        /// <response code="200">Retorna a lista de carteiras do vendedor</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpGet("seller/{sellerId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<WalletDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSellerWallets(Guid sellerId)
        {
            try
            {
                _logger.LogInformation("Obtendo carteiras para o vendedor {SellerId}", sellerId);
                var wallets = await _walletService.GetSellerWalletsAsync(sellerId);
                return Ok(new ApiResponse<IEnumerable<WalletDto>>(wallets));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter carteiras para o vendedor {SellerId}", sellerId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Obtém uma carteira específica pelo tipo (Depósito, Saque ou General)
        /// </summary>
        /// <param name="sellerId">ID do vendedor</param>
        /// <param name="walletType">Tipo da carteira (Deposit, Withdrawal ou General)</param>
        /// <response code="200">Retorna a carteira solicitada</response>
        /// <response code="404">Carteira não encontrada</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpGet("seller/{sellerId:guid}/type/{walletType}")]
        [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWalletByType(Guid sellerId, WalletType walletType)
        {
            try
            {
                _logger.LogInformation("Obtendo carteira do tipo {WalletType} para o vendedor {SellerId}", walletType, sellerId);
                var wallet = await _walletService.GetWalletByTypeAsync(sellerId, walletType);
                return Ok(new ApiResponse<WalletDto>(wallet));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira do tipo {WalletType} não encontrada para o vendedor {SellerId}", walletType, sellerId);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter carteira do tipo {WalletType} para o vendedor {SellerId}", walletType, sellerId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Obtém a carteira com transações recentes
        /// </summary>
        /// <param name="id">ID da carteira</param>
        /// <param name="count">Número de transações recentes a serem retornadas</param>
        /// <response code="200">Retorna a carteira com transações recentes</response>
        /// <response code="404">Carteira não encontrada</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpGet("{id:guid}/with-transactions")]
        [ProducesResponseType(typeof(ApiResponse<WalletWithTransactionsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWalletWithTransactions(Guid id, [FromQuery] int count = 5)
        {
            try
            {
                _logger.LogInformation("Obtendo carteira com transações para o ID {WalletId}", id);
                var walletWithTransactions = await _walletService.GetWalletWithRecentTransactionsAsync(id, count);
                return Ok(new ApiResponse<WalletWithTransactionsDto>(walletWithTransactions));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada com ID {WalletId}", id);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter carteira com transações para o ID {WalletId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Cria uma nova carteira para um vendedor
        /// </summary>
        /// <param name="createDto">Dados para criação da carteira</param>
        /// <response code="201">Carteira criada com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="409">Carteira já existe ou limite de carteiras atingido</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateWallet([FromBody] WalletCreateDto createDto)
        {
            try
            {
                _logger.LogInformation("Criando carteira do tipo {WalletType} para o vendedor {SellerId}",
                    createDto.WalletType, createDto.SellerId);

                var wallet = await _walletService.CreateWalletAsync(createDto);
                return CreatedAtAction(nameof(GetWallet), new { id = wallet.Id }, new ApiResponse<WalletDto>(wallet));
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, "Conflito ao criar carteira do tipo {WalletType} para o vendedor {SellerId}",
                    createDto.WalletType, createDto.SellerId);
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
        /// <param name="id">ID da carteira</param>
        /// <param name="updateDto">Dados para atualização do saldo</param>
        /// <response code="200">Saldo atualizado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="404">Carteira não encontrada</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpPut("{id:guid}/balance")]
        [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateBalance(Guid id, [FromBody] WalletUpdateDto updateDto)
        {
            try
            {
                _logger.LogInformation("Atualizando saldo para a carteira {WalletId}", id);
                var wallet = await _walletService.UpdateBalanceAsync(id, updateDto);
                return Ok(new ApiResponse<WalletDto>(wallet));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada com ID {WalletId}", id);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validação falhou ao atualizar saldo");
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar saldo para a carteira {WalletId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Define uma carteira como padrão
        /// </summary>
        /// <param name="id">ID da carteira</param>
        /// <param name="sellerId">ID do vendedor</param>
        /// <response code="200">Carteira definida como padrão com sucesso</response>
        /// <response code="404">Carteira não encontrada</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpPost("{id:guid}/default")]
        [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SetDefaultWallet(Guid id, [FromQuery] Guid sellerId)
        {
            try
            {
                _logger.LogInformation("Definindo carteira {WalletId} como padrão para o vendedor {SellerId}", id, sellerId);
                var wallet = await _walletService.SetDefaultWalletAsync(id, sellerId);
                return Ok(new ApiResponse<WalletDto>(wallet));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada: {Message}", ex.Message);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao definir carteira padrão: {WalletId}, {SellerId}", id, sellerId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Adiciona fundos a uma carteira
        /// </summary>
        /// <param name="id">ID da carteira</param>
        /// <param name="request">Informações do depósito</param>
        /// <response code="200">Fundos adicionados com sucesso</response>
        /// <response code="400">Valor inválido ou carteira do tipo incorreto</response>
        /// <response code="404">Carteira não encontrada</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpPost("{id:guid}/deposits")]
        [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddFunds(Guid id, [FromBody] FundsOperationRequest request)
        {
            try
            {
                _logger.LogInformation("Adicionando {Amount} à carteira {WalletId}", request.Amount, id);

                var operationDto = new WalletOperationDto
                {
                    Amount = request.Amount,
                    Description = request.Description ?? "Depósito de fundos",
                    Reference = request.Reference
                };

                var wallet = await _walletService.AddFundsAsync(id, operationDto);
                return Ok(new ApiResponse<WalletDto>(wallet));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada com ID {WalletId}", id);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validação falhou ao adicionar fundos: {Message}", ex.Message);
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar fundos para a carteira {WalletId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Retira fundos de uma carteira
        /// </summary>
        /// <param name="id">ID da carteira</param>
        /// <param name="request">Informações da retirada</param>
        /// <response code="200">Fundos retirados com sucesso</response>
        /// <response code="400">Valor inválido, saldo insuficiente ou carteira do tipo incorreto</response>
        /// <response code="404">Carteira não encontrada</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpPost("{id:guid}/withdrawals")]
        [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeductFunds(Guid id, [FromBody] FundsOperationRequest request)
        {
            try
            {
                _logger.LogInformation("Retirando {Amount} da carteira {WalletId}", request.Amount, id);

                var operationDto = new WalletOperationDto
                {
                    Amount = request.Amount,
                    Description = request.Description ?? "Retirada de fundos",
                    Reference = request.Reference
                };

                var wallet = await _walletService.DeductFundsAsync(id, operationDto);
                return Ok(new ApiResponse<WalletDto>(wallet));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada com ID {WalletId}", id);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validação falhou ao retirar fundos: {Message}", ex.Message);
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (InsufficientFundsException ex)
            {
                _logger.LogWarning(ex, "Fundos insuficientes para a carteira {WalletId}", id);
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retirar fundos para a carteira {WalletId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Transfere fundos entre duas carteiras do mesmo vendedor
        /// </summary>
        /// <param name="sourceWalletId">ID da carteira de origem</param>
        /// <param name="destinationWalletId">ID da carteira de destino</param>
        /// <param name="request">Detalhes da transferência</param>
        /// <response code="200">Transferência realizada com sucesso</response>
        /// <response code="400">Valor inválido, saldo insuficiente ou tipos de carteira incompatíveis</response>
        /// <response code="404">Carteira não encontrada</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpPost("transfer")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> TransferBetweenWallets(
            [FromQuery] Guid sourceWalletId,
            [FromQuery] Guid destinationWalletId,
            [FromBody] FundsOperationRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Transferindo {Amount} da carteira {SourceWalletId} para a carteira {DestinationWalletId}",
                    request.Amount, sourceWalletId, destinationWalletId);

                var (sourceWallet, destinationWallet) = await _walletService.TransferBetweenWalletsAsync(
                    sourceWalletId,
                    destinationWalletId,
                    request.Amount,
                    request.Description);

                var result = new
                {
                    SourceWallet = sourceWallet,
                    DestinationWallet = destinationWallet
                };

                return Ok(new ApiResponse<object>(result));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada: {Message}", ex.Message);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validação falhou na transferência: {Message}", ex.Message);
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (InsufficientFundsException ex)
            {
                _logger.LogWarning(ex, "Fundos insuficientes para transferência da carteira {SourceWalletId}", sourceWalletId);
                return BadRequest(new ApiResponse<object>(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao transferir {Amount} da carteira {SourceWalletId} para a carteira {DestinationWalletId}",
                    request.Amount, sourceWalletId, destinationWalletId);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Obtém o histórico de transações de uma carteira
        /// </summary>
        /// <param name="id">ID da carteira</param>
        /// <param name="startDate">Data inicial</param>
        /// <param name="endDate">Data final</param>
        /// <param name="page">Página</param>
        /// <param name="pageSize">Tamanho da página</param>
        /// <response code="200">Histórico obtido com sucesso</response>
        /// <response code="404">Carteira não encontrada</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpGet("{id:guid}/transactions")]
        [ProducesResponseType(typeof(ApiResponse<List<WalletTransactionDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTransactions(
            Guid id,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Obtendo transações para a carteira {WalletId}", id);
                var transactions = await _walletService.GetTransactionsAsync(id, startDate, endDate, page, pageSize);
                return Ok(new ApiResponse<List<WalletTransactionDto>>(transactions));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada com ID {WalletId}", id);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter transações para a carteira {WalletId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }

        /// <summary>
        /// Obtém o saldo disponível de uma carteira
        /// </summary>
        /// <param name="id">ID da carteira</param>
        /// <response code="200">Saldo obtido com sucesso</response>
        /// <response code="404">Carteira não encontrada</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpGet("{id:guid}/balance")]
        [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBalance(Guid id)
        {
            try
            {
                _logger.LogInformation("Obtendo saldo da carteira {WalletId}", id);
                var balance = await _walletService.GetAvailableBalanceAsync(id);
                return Ok(new ApiResponse<decimal>(balance));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Carteira não encontrada com ID {WalletId}", id);
                return NotFound(new ApiResponse<object>(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter saldo da carteira {WalletId}", id);
                return StatusCode(500, new ApiResponse<object>(HttpStatusCode.InternalServerError, "Ocorreu um erro interno ao processar sua solicitação."));
            }
        }
    }
}