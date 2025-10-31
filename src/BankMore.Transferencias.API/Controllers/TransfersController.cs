using BankMore.Transferencias.Application.Commands.CreateTransfer;
using BankMore.Transferencias.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankMore.Transferencias.API.Controllers;

[ApiController]
[Route("api/transferencias")]
[Produces("application/json")]
public class TransfersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TransfersController> _logger;

    public TransfersController(IMediator mediator, ILogger<TransfersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateTransferCommand command)
    {
        try
        {
            var originAccountNumber = HttpContext.Items["AccountNumber"]?.ToString();
            if (string.IsNullOrWhiteSpace(originAccountNumber))
                return BadRequest(new ErrorResponse("USER_UNAUTHORIZED", "Token inválido ou expirado. Número da conta não encontrado."));

            var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new ErrorResponse("USER_UNAUTHORIZED", "Token JWT não fornecido."));

            command.OriginAccountNumber = originAccountNumber;
            command.JwtToken = token;

            var response = await _mediator.Send(command);

            return NoContent();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Erro de domínio ao processar transferência: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
            
            if (ex.ErrorCode == "USER_UNAUTHORIZED" || ex.ErrorCode == "INVALID_TOKEN")
            {
                return StatusCode(403, new ErrorResponse(ex.ErrorCode, ex.Message));
            }
            
            return BadRequest(new ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar transferência");
            return StatusCode(500, new ErrorResponse("INTERNAL_ERROR", "Erro interno do servidor."));
        }
    }
}

public class ErrorResponse
{
    public string ErrorCode { get; set; }
    public string Message { get; set; }

    public ErrorResponse(string errorCode, string message)
    {
        ErrorCode = errorCode;
        Message = message;
    }
}

