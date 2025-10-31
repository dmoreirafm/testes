using MediatR;
using System.ComponentModel.DataAnnotations;

namespace BankMore.Contas.Application.Commands.DeactivateAccount;

/// <summary>
/// Comando para inativar uma conta corrente
/// </summary>
public class DeactivateAccountCommand : IRequest<DeactivateAccountResponse>
{
    /// <summary>
    /// Senha da conta para confirmação
    /// </summary>
    /// <example>senha123</example>
    [Required(ErrorMessage = "Senha é obrigatória para confirmação")]
    public string Password { get; set; } = string.Empty;
}

