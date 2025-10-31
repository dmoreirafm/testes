using MediatR;
using System.ComponentModel.DataAnnotations;

namespace BankMore.Contas.Application.Commands.Login;

/// <summary>
/// Comando para realizar login e obter token JWT
/// </summary>
public class LoginCommand : IRequest<LoginResponse>
{
    /// <summary>
    /// CPF (11 dígitos) ou número da conta (10 dígitos) do usuário
    /// </summary>
    /// <example>12345678909</example>
    [Required(ErrorMessage = "Login (CPF ou número da conta) é obrigatório")]
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Senha da conta
    /// </summary>
    /// <example>senha123</example>
    [Required(ErrorMessage = "Senha é obrigatória")]
    public string Password { get; set; } = string.Empty;
}

