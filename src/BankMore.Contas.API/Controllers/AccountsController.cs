using BankMore.Contas.Application.Commands.DeactivateAccount;
using BankMore.Contas.Application.Commands.Login;
using BankMore.Contas.Application.Commands.MakeTransaction;
using BankMore.Contas.Application.Commands.RegisterAccount;
using BankMore.Contas.Application.Queries.GetBalance;
using BankMore.Contas.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankMore.Contas.API.Controllers;

[ApiController]
[Route("api/contas")]
[Produces("application/json")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(IMediator mediator, ILogger<AccountsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("cadastrar")]
    [ProducesResponseType(typeof(RegisterAccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterAccountCommand command)
    {
        try
        {
            _logger.LogInformation("Recebendo requisição de cadastro para CPF: {Cpf}", command.Cpf);
            var response = await _mediator.Send(command);
            _logger.LogInformation("Conta cadastrada com sucesso: {AccountNumber}", response.AccountNumber);
            return CreatedAtAction(nameof(Register), response);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Erro de domínio ao cadastrar conta: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
            return BadRequest(new ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cadastrar conta");
            return StatusCode(500, new ErrorResponse("INTERNAL_ERROR", "Erro interno do servidor."));
        }
    }

    [HttpPost("entrar")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        try
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }
        catch (DomainException ex)
        {
            return StatusCode(401, new ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao realizar login");
            return StatusCode(500, new ErrorResponse("INTERNAL_ERROR", "Erro interno do servidor."));
        }
    }

    [HttpGet("saldo")]
    [Authorize]
    [ProducesResponseType(typeof(GetBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBalance([FromQuery] string? accountNumber = null)
    {
        try
        {
            var accountNumberFromToken = HttpContext.Items["AccountNumber"]?.ToString();
            if (string.IsNullOrWhiteSpace(accountNumberFromToken) && string.IsNullOrWhiteSpace(accountNumber))
            {
                return StatusCode(403, new ErrorResponse("USER_UNAUTHORIZED", "Token inválido ou expirado."));
            }

            var finalAccountNumber = accountNumber ?? accountNumberFromToken;

            if (string.IsNullOrWhiteSpace(finalAccountNumber))
                return StatusCode(403, new ErrorResponse("USER_UNAUTHORIZED", "Token inválido ou expirado. Número da conta não encontrado."));

            var query = new GetBalanceQuery { AccountNumber = finalAccountNumber };
            var response = await _mediator.Send(query);
            return Ok(response);
        }
        catch (DomainException ex)
        {
            if (ex.ErrorCode == "USER_UNAUTHORIZED" || ex.ErrorCode == "INVALID_TOKEN")
            {
                return StatusCode(403, new ErrorResponse(ex.ErrorCode, ex.Message));
            }
            
            return BadRequest(new ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar saldo");
            return StatusCode(500, new ErrorResponse("INTERNAL_ERROR", "Erro interno do servidor."));
        }
    }

    [HttpPost("movimentacoes")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MakeTransaction([FromBody] MakeTransactionCommand command)
    {
        try
        {
            var accountNumberFromToken = HttpContext.Items["AccountNumber"]?.ToString();
            
            if (string.IsNullOrWhiteSpace(command.AccountNumber))
            {
                if (string.IsNullOrWhiteSpace(accountNumberFromToken))
                    return StatusCode(403, new ErrorResponse("USER_UNAUTHORIZED", "Token inválido ou expirado."));
                
                command.AccountNumber = accountNumberFromToken;
            }

            if (command.Type == 'D' && !string.IsNullOrWhiteSpace(accountNumberFromToken) && 
                command.AccountNumber != accountNumberFromToken)
            {
                return BadRequest(new ErrorResponse("INVALID_TYPE", "Apenas créditos podem ser realizados em contas diferentes da sua."));
            }

            await _mediator.Send(command);
            return NoContent();
        }
        catch (DomainException ex)
        {
            if (ex.ErrorCode == "USER_UNAUTHORIZED" || ex.ErrorCode == "INVALID_TOKEN")
            {
                return StatusCode(403, new ErrorResponse(ex.ErrorCode, ex.Message));
            }
            
            return BadRequest(new ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao realizar movimentação");
            return StatusCode(500, new ErrorResponse("INTERNAL_ERROR", "Erro interno do servidor."));
        }
    }

    [HttpPost("inativar")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Deactivate([FromBody] DeactivateAccountCommand command)
    {
        try
        {
            var accountIdStr = HttpContext.Items["AccountId"]?.ToString();
            if (string.IsNullOrWhiteSpace(accountIdStr) || !int.TryParse(accountIdStr, out int accountId))
                return StatusCode(403, new ErrorResponse("USER_UNAUTHORIZED", "Token inválido ou expirado."));

            var handler = HttpContext.RequestServices.GetRequiredService<DeactivateAccountCommandHandler>();
            await handler.HandleWithAccountId(accountId, command, CancellationToken.None);
            return NoContent();
        }
        catch (DomainException ex)
        {
            if (ex.ErrorCode == "USER_UNAUTHORIZED" || ex.ErrorCode == "INVALID_TOKEN")
            {
                return StatusCode(403, new ErrorResponse(ex.ErrorCode, ex.Message));
            }
            
            return BadRequest(new ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inativar conta");
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

