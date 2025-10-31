using BankMore.Tarifas.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BankMore.Tarifas.API.Controllers;

[ApiController]
[Route("api/tarifas")]
[Produces("application/json")]
public class FeesController : ControllerBase
{
    private readonly IFeeRepository _feeRepository;
    private readonly ILogger<FeesController> _logger;

    public FeesController(IFeeRepository feeRepository, ILogger<FeesController> logger)
    {
        _feeRepository = feeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Obtém uma tarifa específica pelo ID da transferência
    /// </summary>
    /// <remarks>
    /// Este endpoint permite consultar uma tarifa aplicada usando o ID da transferência.
    /// 
    /// **Parâmetros:**
    /// - transferId: ID único da transferência (RequestId)
    /// 
    /// **Retornos:**
    /// - 200 OK: Tarifa encontrada
    /// - 404 Not Found: Tarifa não encontrada para a transferência informada
    /// </remarks>
    /// <param name="transferId">ID único da transferência (RequestId)</param>
    /// <returns>Tarifa aplicada com detalhes completos</returns>
    /// <response code="200">Tarifa encontrada e retornada com sucesso.</response>
    /// <response code="404">Tarifa não encontrada para a transferência informada.</response>
    [HttpGet("transferencia/{transferId}")]
    [ProducesResponseType(typeof(FeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByTransferId(string transferId)
    {
        var fee = await _feeRepository.GetByTransferIdAsync(transferId);

        if (fee == null)
        {
            return NotFound(new { message = $"Tarifa não encontrada para transferência {transferId}" });
        }

        return Ok(new FeeResponse
        {
            Id = fee.Id,
            TransferId = fee.TransferId,
            AccountNumber = fee.AccountNumber,
            TransferAmount = fee.TransferAmount,
            FeeAmount = fee.FeeAmount,
            AppliedAt = fee.AppliedAt
        });
    }

    /// <summary>
    /// Lista todas as tarifas aplicadas em uma conta
    /// </summary>
    /// <remarks>
    /// Este endpoint retorna todas as tarifas aplicadas em uma conta corrente específica.
    /// As tarifas são aplicadas automaticamente quando uma transferência é realizada.
    /// 
    /// **Parâmetros:**
    /// - accountNumber: Número da conta corrente (10 dígitos)
    /// 
    /// **Ordenação:**
    /// - Tarifas são ordenadas por data de aplicação (mais recentes primeiro)
    /// - Limite máximo de 100 registros retornados
    /// 
    /// **Retornos:**
    /// - 200 OK: Lista de tarifas aplicadas na conta
    /// </remarks>
    /// <param name="accountNumber">Número da conta corrente (10 dígitos)</param>
    /// <returns>Lista de tarifas aplicadas na conta, ordenadas por data (mais recentes primeiro)</returns>
    /// <response code="200">Lista de tarifas retornada com sucesso.</response>
    [HttpGet("conta/{accountNumber}")]
    [ProducesResponseType(typeof(IEnumerable<FeeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByAccountNumber(string accountNumber)
    {
        try
        {
            _logger.LogInformation("Consultando tarifas para conta: {AccountNumber}", accountNumber);
            
            var fees = await _feeRepository.GetByAccountNumberAsync(accountNumber);

            var response = fees.Select(fee => new FeeResponse
            {
                Id = fee.Id,
                TransferId = fee.TransferId,
                AccountNumber = fee.AccountNumber,
                TransferAmount = fee.TransferAmount,
                FeeAmount = fee.FeeAmount,
                AppliedAt = fee.AppliedAt
            });

            _logger.LogInformation("Encontradas {Count} tarifas para conta {AccountNumber}", response.Count(), accountNumber);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar tarifas para conta {AccountNumber}", accountNumber);
            throw;
        }
    }

    /// <summary>
    /// Lista todas as tarifas aplicadas no sistema (com paginação)
    /// </summary>
    /// <remarks>
    /// Este endpoint retorna todas as tarifas aplicadas no sistema com suporte a paginação.
    /// 
    /// **Parâmetros:**
    /// - skip: Número de registros a pular (padrão: 0)
    /// - take: Número de registros a retornar (padrão: 100, máximo: 100)
    /// 
    /// **Ordenação:**
    /// - Tarifas são ordenadas por data de aplicação (mais recentes primeiro)
    /// 
    /// **Validações:**
    /// - take será limitado a 100 se informado valor maior
    /// - take mínimo é 1 (ajustado para 10 se menor)
    /// - skip não pode ser negativo (ajustado para 0)
    /// 
    /// **Retornos:**
    /// - 200 OK: Lista de tarifas retornada com sucesso
    /// </remarks>
    /// <param name="skip">Número de registros a pular (padrão: 0)</param>
    /// <param name="take">Número de registros a retornar (padrão: 100, máximo: 100)</param>
    /// <returns>Lista paginada de tarifas, ordenadas por data (mais recentes primeiro)</returns>
    /// <response code="200">Lista de tarifas retornada com sucesso.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FeeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 100)
    {
        if (take > 100)
            take = 100;
        if (take < 1)
            take = 10;
        if (skip < 0)
            skip = 0;

        var fees = await _feeRepository.GetAllAsync(skip, take);

        var response = fees.Select(fee => new FeeResponse
        {
            Id = fee.Id,
            TransferId = fee.TransferId,
            AccountNumber = fee.AccountNumber,
            TransferAmount = fee.TransferAmount,
            FeeAmount = fee.FeeAmount,
            AppliedAt = fee.AppliedAt
        });

        return Ok(response);
    }

    /// <summary>
    /// Verifica se uma tarifa foi aplicada para uma transferência
    /// </summary>
    /// <remarks>
    /// Este endpoint permite verificar se uma tarifa já foi aplicada para uma transferência específica.
    /// Útil para garantir idempotência e evitar aplicação duplicada de tarifas.
    /// 
    /// **Parâmetros:**
    /// - transferId: ID único da transferência (RequestId)
    /// 
    /// **Retornos:**
    /// - 200 OK: Retorna true se a tarifa existe, false caso contrário
    /// </remarks>
    /// <param name="transferId">ID único da transferência (RequestId)</param>
    /// <returns>Objeto indicando se a tarifa existe para a transferência informada</returns>
    /// <response code="200">Verificação realizada com sucesso.</response>
    [HttpGet("existe/{transferId}")]
    [ProducesResponseType(typeof(FeeExistsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Exists(string transferId)
    {
        var exists = await _feeRepository.ExistsByTransferIdAsync(transferId);

        return Ok(new FeeExistsResponse
        {
            TransferId = transferId,
            Exists = exists
        });
    }
}

/// <summary>
/// Resposta com dados de uma tarifa aplicada
/// </summary>
public class FeeResponse
{
    /// <summary>
    /// ID interno da tarifa
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// ID único da transferência que gerou a tarifa
    /// </summary>
    /// <example>550e8400-e29b-41d4-a716-446655440000</example>
    public string TransferId { get; set; } = string.Empty;

    /// <summary>
    /// Número da conta corrente onde a tarifa foi aplicada
    /// </summary>
    /// <example>1234567890</example>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Valor da transferência que gerou a tarifa
    /// </summary>
    /// <example>100.50</example>
    public decimal TransferAmount { get; set; }

    /// <summary>
    /// Valor da tarifa aplicada (fixo: R$ 2,00)
    /// </summary>
    /// <example>2.00</example>
    public decimal FeeAmount { get; set; }

    /// <summary>
    /// Data e hora em que a tarifa foi aplicada (UTC)
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime AppliedAt { get; set; }
}

/// <summary>
/// Resposta indicando se uma tarifa existe para uma transferência
/// </summary>
public class FeeExistsResponse
{
    /// <summary>
    /// ID único da transferência verificada
    /// </summary>
    /// <example>550e8400-e29b-41d4-a716-446655440000</example>
    public string TransferId { get; set; } = string.Empty;

    /// <summary>
    /// Indica se a tarifa existe para a transferência
    /// </summary>
    /// <example>true</example>
    public bool Exists { get; set; }
}

