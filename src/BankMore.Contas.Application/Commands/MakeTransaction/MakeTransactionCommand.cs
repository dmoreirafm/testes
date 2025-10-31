using MediatR;
using System.ComponentModel.DataAnnotations;

namespace BankMore.Contas.Application.Commands.MakeTransaction;

public class MakeTransactionCommand : IRequest<MakeTransactionResponse>
{
    [Required(ErrorMessage = "RequestId é obrigatório")]
    public string RequestId { get; set; } = string.Empty;

    public string? AccountNumber { get; set; }

    [Required(ErrorMessage = "Valor é obrigatório")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Tipo é obrigatório")]
    [RegularExpression("^[CD]$", ErrorMessage = "Tipo deve ser 'C' (Crédito) ou 'D' (Débito)")]
    public char Type { get; set; }
}

