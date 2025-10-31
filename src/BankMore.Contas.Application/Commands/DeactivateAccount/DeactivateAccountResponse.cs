namespace BankMore.Contas.Application.Commands.DeactivateAccount;

/// <summary>
/// Resposta da inativação de conta corrente
/// </summary>
public class DeactivateAccountResponse
{
    /// <summary>
    /// Número da conta inativada
    /// </summary>
    /// <example>1234567890</example>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Mensagem de confirmação
    /// </summary>
    /// <example>Conta inativada com sucesso.</example>
    public string Message { get; set; } = string.Empty;
}

