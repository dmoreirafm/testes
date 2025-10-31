using MediatR;
using System.ComponentModel.DataAnnotations;

namespace BankMore.Transferencias.Application.Commands.CreateTransfer;

/// <summary>
/// Comando para realizar uma transferência entre contas da mesma instituição
/// </summary>
public class CreateTransferCommand : IRequest<CreateTransferResponse>
{
    /// <summary>
    /// Identificador único da requisição para garantir idempotência
    /// </summary>
    /// <example>550e8400-e29b-41d4-a716-446655440000</example>
    [Required(ErrorMessage = "RequestId é obrigatório")]
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Número da conta de origem (preenchido automaticamente do token JWT)
    /// </summary>
    /// <example>1234567890</example>
    public string OriginAccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Número da conta de destino (10 dígitos)
    /// </summary>
    /// <example>0987654321</example>
    [Required(ErrorMessage = "Conta de destino é obrigatória")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Número da conta deve ter 10 dígitos")]
    public string DestinationAccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Valor a ser transferido (deve ser positivo)
    /// </summary>
    /// <example>100.50</example>
    [Required(ErrorMessage = "Valor é obrigatório")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Token JWT para autenticação (preenchido automaticamente pelo controller)
    /// </summary>
    public string JwtToken { get; set; } = string.Empty;
}

