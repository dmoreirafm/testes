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

    /// <summary>
    /// Cadastra uma nova conta corrente
    /// </summary>
    /// <remarks>
    /// Este endpoint permite cadastrar uma nova conta corrente na BankMore.
    /// O CPF será validado usando o algoritmo completo de validação.
    /// Um número de conta único (10 dígitos) será gerado automaticamente.
    /// 
    /// **Validações:**
    /// - CPF deve ser válido (algoritmo completo)
    /// - CPF não pode estar já cadastrado
    /// - Nome deve ter entre 3 e 200 caracteres
    /// - Senha deve ter no mínimo 6 caracteres
    /// 
    /// **Retornos:**
    /// - 201 Created: Conta cadastrada com sucesso
    /// - 400 Bad Request: CPF inválido (INVALID_DOCUMENT) ou dados inconsistentes
    /// </remarks>
    /// <param name="command">Dados do cadastro (CPF, senha e nome)</param>
    /// <returns>Número da conta gerada</returns>
    /// <response code="201">Conta cadastrada com sucesso. Retorna o número da conta.</response>
    /// <response code="400">CPF inválido ou dados inconsistentes.</response>
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

    /// <summary>
    /// Realiza login e retorna token JWT
    /// </summary>
    /// <remarks>
    /// Este endpoint permite autenticar um usuário e obter um token JWT para acesso às APIs protegidas.
    /// 
    /// **Login pode ser feito com:**
    /// - CPF (11 dígitos)
    /// - Número da conta (10 dígitos)
    /// 
    /// **Validações:**
    /// - Login e senha devem estar corretos
    /// - Conta deve estar ativa
    /// 
    /// **Token JWT:**
    /// - Deve ser enviado no header Authorization: "Bearer {token}"
    /// - Expira em 24 horas (configurável)
    /// - Contém AccountId e AccountNumber
    /// 
    /// **Retornos:**
    /// - 200 OK: Login realizado com sucesso. Retorna token JWT.
    /// - 401 Unauthorized: Credenciais inválidas (USER_UNAUTHORIZED) ou conta inativa (INACTIVE_ACCOUNT)
    /// </remarks>
    /// <param name="command">Dados de login (CPF/conta e senha)</param>
    /// <returns>Token JWT e informações da conta</returns>
    /// <response code="200">Login realizado com sucesso. Retorna token JWT.</response>
    /// <response code="401">Credenciais inválidas ou conta inativa.</response>
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

    /// <summary>
    /// Consulta o saldo da conta corrente
    /// </summary>
    /// <remarks>
    /// Este endpoint permite consultar o saldo atual de uma conta corrente.
    /// 
    /// **Autenticação:**
    /// - Requer token JWT válido no header Authorization
    /// 
    /// **Parâmetros:**
    /// - accountNumber (opcional): Se não informado, usa a conta do token JWT
    /// 
    /// **Cálculo do Saldo:**
    /// - Saldo = Soma de todos os créditos - Soma de todos os débitos
    /// - Retorna 0,00 se não houver movimentações
    /// 
    /// **Validações:**
    /// - Conta deve existir (INVALID_ACCOUNT)
    /// - Conta deve estar ativa (INACTIVE_ACCOUNT)
    /// 
    /// **Retornos:**
    /// - 200 OK: Retorna saldo e informações da conta
    /// - 400 Bad Request: Conta inválida ou inativa
    /// - 403 Forbidden: Token inválido ou expirado
    /// </remarks>
    /// <param name="accountNumber">Número da conta (opcional - usa do token se não informado)</param>
    /// <returns>Saldo atual e informações da conta</returns>
    /// <response code="200">Saldo consultado com sucesso.</response>
    /// <response code="400">Conta inválida ou inativa.</response>
    /// <response code="403">Token inválido ou expirado.</response>
    [HttpGet("saldo")]
    [Authorize]
    [ProducesResponseType(typeof(GetBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBalance([FromQuery] string? accountNumber = null)
    {
        try
        {
            // Verifica se o token é válido (se não houver AccountNumber no token, significa que o token está inválido)
            var accountNumberFromToken = HttpContext.Items["AccountNumber"]?.ToString();
            if (string.IsNullOrWhiteSpace(accountNumberFromToken) && string.IsNullOrWhiteSpace(accountNumber))
            {
                // Se não há AccountNumber no token e não foi informado como parâmetro, o token está inválido
                return StatusCode(403, new ErrorResponse("USER_UNAUTHORIZED", "Token inválido ou expirado."));
            }

            // Se não informado, usa do token
            var finalAccountNumber = accountNumber ?? accountNumberFromToken;

            if (string.IsNullOrWhiteSpace(finalAccountNumber))
                return StatusCode(403, new ErrorResponse("USER_UNAUTHORIZED", "Token inválido ou expirado. Número da conta não encontrado."));

            var query = new GetBalanceQuery { AccountNumber = finalAccountNumber };
            var response = await _mediator.Send(query);
            return Ok(response);
        }
        catch (DomainException ex)
        {
            // Retorna 403 se for erro de autorização
            if (ex.ErrorCode == "USER_UNAUTHORIZED" || ex.ErrorCode == "INVALID_TOKEN")
            {
                return StatusCode(403, new ErrorResponse(ex.ErrorCode, ex.Message));
            }
            
            // Retorna 400 para outros erros de validação (INVALID_ACCOUNT, INACTIVE_ACCOUNT)
            return BadRequest(new ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar saldo");
            return StatusCode(500, new ErrorResponse("INTERNAL_ERROR", "Erro interno do servidor."));
        }
    }

    /// <summary>
    /// Realiza uma movimentação na conta corrente (crédito ou débito)
    /// </summary>
    /// <remarks>
    /// Este endpoint permite realizar depósitos (crédito) ou saques (débito) em uma conta corrente.
    /// 
    /// **Autenticação:**
    /// - Requer token JWT válido no header Authorization
    /// 
    /// **Parâmetros:**
    /// - accountNumber (opcional): Se não informado, usa a conta do token JWT
    /// - Type: 'C' para Crédito (depósito) ou 'D' para Débito (saque)
    /// 
    /// **Regras de Negócio:**
    /// - Apenas créditos podem ser feitos em contas diferentes da logada
    /// - Débitos só podem ser feitos na própria conta
    /// - Valores devem ser positivos
    /// 
    /// **Validações:**
    /// - Conta deve existir (INVALID_ACCOUNT)
    /// - Conta deve estar ativa (INACTIVE_ACCOUNT)
    /// - Valor deve ser positivo (INVALID_VALUE)
    /// - Tipo deve ser 'C' ou 'D' (INVALID_TYPE)
    /// 
    /// **Idempotência:**
    /// - RequestId garante que a mesma transação não será processada duas vezes
    /// 
    /// **Retornos:**
    /// - 204 No Content: Movimentação realizada com sucesso
    /// - 400 Bad Request: Dados inconsistentes ou validações falharam
    /// - 403 Forbidden: Token inválido ou expirado
    /// </remarks>
    /// <param name="command">Dados da movimentação (RequestId, AccountNumber opcional, Amount, Type)</param>
    /// <returns>Nenhum conteúdo (204)</returns>
    /// <response code="204">Movimentação realizada com sucesso.</response>
    /// <response code="400">Dados inconsistentes ou validações falharam.</response>
    /// <response code="403">Token inválido ou expirado.</response>
    [HttpPost("movimentacoes")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MakeTransaction([FromBody] MakeTransactionCommand command)
    {
        try
        {
            // Verifica se o token é válido (se não houver AccountNumber no token, significa que o token está inválido)
            var accountNumberFromToken = HttpContext.Items["AccountNumber"]?.ToString();
            
            // Se AccountNumber não foi informado e não está no token, o token está inválido
            if (string.IsNullOrWhiteSpace(command.AccountNumber))
            {
                if (string.IsNullOrWhiteSpace(accountNumberFromToken))
                    return StatusCode(403, new ErrorResponse("USER_UNAUTHORIZED", "Token inválido ou expirado."));
                
                command.AccountNumber = accountNumberFromToken;
            }

            // Validação: apenas créditos podem ser feitos em contas diferentes da logada
            // Se for débito e a conta for diferente da logada, retorna erro
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
            // Retorna 403 se for erro de autorização
            if (ex.ErrorCode == "USER_UNAUTHORIZED" || ex.ErrorCode == "INVALID_TOKEN")
            {
                return StatusCode(403, new ErrorResponse(ex.ErrorCode, ex.Message));
            }
            
            // Retorna 400 para outros erros de validação (INVALID_ACCOUNT, INACTIVE_ACCOUNT, INVALID_VALUE, INVALID_TYPE)
            return BadRequest(new ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao realizar movimentação");
            return StatusCode(500, new ErrorResponse("INTERNAL_ERROR", "Erro interno do servidor."));
        }
    }

    /// <summary>
    /// Inativa uma conta corrente (requer confirmação por senha)
    /// </summary>
    /// <remarks>
    /// Este endpoint permite inativar uma conta corrente. Requer confirmação com a senha da conta.
    /// 
    /// **Autenticação:**
    /// - Requer token JWT válido no header Authorization
    /// 
    /// **Confirmação:**
    /// - Senha deve ser fornecida e validada
    /// 
    /// **Validações:**
    /// - Conta deve existir (INVALID_ACCOUNT)
    /// - Conta não deve estar já inativa
    /// - Senha deve estar correta (USER_UNAUTHORIZED)
    /// 
    /// **Efeito:**
    /// - Campo Status da conta será alterado para Inactive (ATIVO = 0)
    /// - Conta inativa não pode receber novas movimentações
    /// 
    /// **Retornos:**
    /// - 204 No Content: Conta inativada com sucesso
    /// - 400 Bad Request: Conta inválida, já inativa ou senha incorreta
    /// - 403 Forbidden: Token inválido ou expirado
    /// </remarks>
    /// <param name="command">Dados da inativação (senha para confirmação)</param>
    /// <returns>Nenhum conteúdo (204)</returns>
    /// <response code="204">Conta inativada com sucesso.</response>
    /// <response code="400">Conta inválida, já inativa ou senha incorreta.</response>
    /// <response code="403">Token inválido ou expirado.</response>
    [HttpPost("inativar")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Deactivate([FromBody] DeactivateAccountCommand command)
    {
        try
        {
            // Obtém accountId do token
            var accountIdStr = HttpContext.Items["AccountId"]?.ToString();
            if (string.IsNullOrWhiteSpace(accountIdStr) || !int.TryParse(accountIdStr, out int accountId))
                return StatusCode(403, new ErrorResponse("USER_UNAUTHORIZED", "Token inválido ou expirado."));

            var handler = HttpContext.RequestServices.GetRequiredService<DeactivateAccountCommandHandler>();
            await handler.HandleWithAccountId(accountId, command, CancellationToken.None);
            return NoContent();
        }
        catch (DomainException ex)
        {
            // Retorna 403 se for erro de autorização
            if (ex.ErrorCode == "USER_UNAUTHORIZED" || ex.ErrorCode == "INVALID_TOKEN")
            {
                return StatusCode(403, new ErrorResponse(ex.ErrorCode, ex.Message));
            }
            
            // Retorna 400 para outros erros de validação (INVALID_ACCOUNT)
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

