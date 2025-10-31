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

public class FeeResponse
{
    public int Id { get; set; }
    public string TransferId { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal TransferAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public DateTime AppliedAt { get; set; }
}

public class FeeExistsResponse
{
    public string TransferId { get; set; } = string.Empty;
    public bool Exists { get; set; }
}

