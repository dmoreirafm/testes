using MediatR;
using System.ComponentModel.DataAnnotations;

namespace BankMore.Contas.Application.Commands.MakeTransaction;

/// <summary>
/// Comando para realizar uma movimentação na conta (crédito ou débito)
/// </summary>
public class MakeTransactionCommand : IRequest<MakeTransactionResponse>
{
    /// <summary>
    /// Identificador único da requisição para garantir idempotência
    /// </summary>
    /// <example>550e8400-e29b-41d4-a716-446655440000</example>
    [Required(ErrorMessage = "RequestId é obrigatório")]
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Número da conta corrente (opcional - se não informado, usa do token JWT)
    /// </summary>
    /// <example>1234567890</example>
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Valor da movimentação (deve ser positivo)
    /// </summary>
    /// <example>100.50</example>
    [Required(ErrorMessage = "Valor é obrigatório")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Tipo de movimentação: 'C' para Crédito (depósito) ou 'D' para Débito (saque)
    /// </summary>
    /// <example>C</example>
    [Required(ErrorMessage = "Tipo é obrigatório")]
    [RegularExpression("^[CD]$", ErrorMessage = "Tipo deve ser 'C' (Crédito) ou 'D' (Débito)")]
    public char Type { get; set; }
}

