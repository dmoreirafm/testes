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

    /// <summary>
    /// Realiza uma transferência entre contas da mesma instituição
    /// </summary>
    /// <remarks>
    /// Este endpoint permite transferir valores entre contas correntes da BankMore.
    /// 
    /// **Autenticação:**
    /// - Requer token JWT válido no header Authorization
    /// - Conta de origem é obtida automaticamente do token
    /// 
    /// **Processo:**
    /// 1. Valida conta de origem (do token) e conta de destino
    /// 2. Realiza débito na conta de origem (via Accounts API)
    /// 3. Realiza crédito na conta de destino (via Accounts API)
    /// 4. Publica mensagem no Kafka para aplicação de tarifa
    /// 5. Em caso de falha no crédito, faz estorno (compensação) automático
    /// 
    /// **Validações:**
    /// - Conta de origem e destino devem existir (INVALID_ACCOUNT)
    /// - Contas devem estar ativas (INACTIVE_ACCOUNT)
    /// - Valor deve ser positivo (INVALID_VALUE)
    /// - Conta de origem e destino devem ser diferentes
    /// 
    /// **Idempotência:**
    /// - RequestId garante que a mesma transferência não será processada duas vezes
    /// 
    /// **Compensação:**
    /// - Se o crédito na conta destino falhar, o débito é automaticamente estornado
    /// 
    /// **Retornos:**
    /// - 204 No Content: Transferência realizada com sucesso
    /// - 400 Bad Request: Dados inconsistentes ou validações falharam
    /// - 403 Forbidden: Token inválido ou expirado
    /// </remarks>
    /// <param name="command">Dados da transferência (RequestId, DestinationAccountNumber, Amount)</param>
    /// <returns>Nenhum conteúdo (204)</returns>
    /// <response code="204">Transferência realizada com sucesso.</response>
    /// <response code="400">Dados inconsistentes ou validações falharam.</response>
    /// <response code="403">Token inválido ou expirado.</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateTransferCommand command)
    {
        try
        {
            // Obtém número da conta de origem do token JWT
            var originAccountNumber = HttpContext.Items["AccountNumber"]?.ToString();
            if (string.IsNullOrWhiteSpace(originAccountNumber))
                return BadRequest(new ErrorResponse("USER_UNAUTHORIZED", "Token inválido ou expirado. Número da conta não encontrado."));

            // Obtém token JWT do header
            var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new ErrorResponse("USER_UNAUTHORIZED", "Token JWT não fornecido."));

            // Preenche dados do command
            command.OriginAccountNumber = originAccountNumber;
            command.JwtToken = token;

            // Usa MediatR para processar o comando
            var response = await _mediator.Send(command);

            return NoContent();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Erro de domínio ao processar transferência: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
            
            // Retorna 403 se for erro de autorização
            if (ex.ErrorCode == "USER_UNAUTHORIZED" || ex.ErrorCode == "INVALID_TOKEN")
            {
                return StatusCode(403, new ErrorResponse(ex.ErrorCode, ex.Message));
            }
            
            // Retorna 400 para erros de validação (INVALID_ACCOUNT, INACTIVE_ACCOUNT, INVALID_VALUE, etc)
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

