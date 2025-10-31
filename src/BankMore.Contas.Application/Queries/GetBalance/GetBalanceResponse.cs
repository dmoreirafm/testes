namespace BankMore.Contas.Application.Queries.GetBalance;

/// <summary>
/// Resposta da consulta de saldo da conta corrente
/// </summary>
public class GetBalanceResponse
{
    /// <summary>
    /// Número da conta corrente consultada
    /// </summary>
    /// <example>1234567890</example>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Nome completo do titular da conta
    /// </summary>
    /// <example>João Silva</example>
    public string AccountHolderName { get; set; } = string.Empty;

    /// <summary>
    /// Saldo atual da conta (soma de créditos menos soma de débitos)
    /// </summary>
    /// <example>1500.00</example>
    public decimal Balance { get; set; }

    /// <summary>
    /// Data e hora da consulta (UTC)
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime ConsultedAt { get; set; }
}

