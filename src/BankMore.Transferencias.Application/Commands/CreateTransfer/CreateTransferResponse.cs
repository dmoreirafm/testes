namespace BankMore.Transferencias.Application.Commands.CreateTransfer;

/// <summary>
/// Resposta da transferência realizada (usado apenas internamente - endpoint retorna 204)
/// </summary>
public class CreateTransferResponse
{
    /// <summary>
    /// ID interno da transferência
    /// </summary>
    /// <example>1</example>
    public string TransferId { get; set; } = string.Empty;

    /// <summary>
    /// Número da conta de origem
    /// </summary>
    /// <example>1234567890</example>
    public string OriginAccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Número da conta de destino
    /// </summary>
    /// <example>0987654321</example>
    public string DestinationAccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Valor transferido
    /// </summary>
    /// <example>100.50</example>
    public decimal Amount { get; set; }

    /// <summary>
    /// Status da transferência: "Completed", "Pending", "Failed", "Compensated"
    /// </summary>
    /// <example>Completed</example>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Data e hora da transferência (UTC)
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime CreatedAt { get; set; }
}

