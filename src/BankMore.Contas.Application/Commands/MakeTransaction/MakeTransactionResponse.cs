namespace BankMore.Contas.Application.Commands.MakeTransaction;

/// <summary>
/// Resposta da movimentação realizada
/// </summary>
public class MakeTransactionResponse
{
    /// <summary>
    /// ID interno da transação
    /// </summary>
    /// <example>1</example>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Número da conta corrente
    /// </summary>
    /// <example>1234567890</example>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Valor movimentado
    /// </summary>
    /// <example>100.50</example>
    public decimal Amount { get; set; }

    /// <summary>
    /// Tipo da transação: "C" para Crédito ou "D" para Débito
    /// </summary>
    /// <example>C</example>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Novo saldo da conta após a movimentação
    /// </summary>
    /// <example>1500.00</example>
    public decimal NewBalance { get; set; }

    /// <summary>
    /// Data e hora da transação (UTC)
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime CreatedAt { get; set; }
}

