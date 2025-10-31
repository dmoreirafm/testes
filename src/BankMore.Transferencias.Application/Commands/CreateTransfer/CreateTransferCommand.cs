using MediatR;
using System.ComponentModel.DataAnnotations;

namespace BankMore.Transferencias.Application.Commands.CreateTransfer;

public class CreateTransferCommand : IRequest<CreateTransferResponse>
{
    [Required(ErrorMessage = "RequestId é obrigatório")]
    public string RequestId { get; set; } = string.Empty;

    public string OriginAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Conta de destino é obrigatória")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Número da conta deve ter 10 dígitos")]
    public string DestinationAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valor é obrigatório")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    public decimal Amount { get; set; }

    public string JwtToken { get; set; } = string.Empty;
}

