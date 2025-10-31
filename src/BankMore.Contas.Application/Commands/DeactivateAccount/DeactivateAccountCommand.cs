using MediatR;
using System.ComponentModel.DataAnnotations;

namespace BankMore.Contas.Application.Commands.DeactivateAccount;

public class DeactivateAccountCommand : IRequest<DeactivateAccountResponse>
{
    [Required(ErrorMessage = "Senha é obrigatória para confirmação")]
    public string Password { get; set; } = string.Empty;
}

