using MediatR;
using System.ComponentModel.DataAnnotations;

namespace BankMore.Contas.Application.Commands.Login;

public class LoginCommand : IRequest<LoginResponse>
{
    [Required(ErrorMessage = "Login (CPF ou número da conta) é obrigatório")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    public string Password { get; set; } = string.Empty;
}

