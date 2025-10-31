namespace BankMore.Contas.Application.Commands.RegisterAccount;

/// <summary>
/// Resposta do cadastro de conta corrente
/// </summary>
public class RegisterAccountResponse
{
    /// <summary>
    /// Número da conta corrente gerado (10 dígitos)
    /// </summary>
    /// <example>1234567890</example>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Mensagem de confirmação
    /// </summary>
    /// <example>Conta cadastrada com sucesso.</example>
    public string Message { get; set; } = string.Empty;
}

