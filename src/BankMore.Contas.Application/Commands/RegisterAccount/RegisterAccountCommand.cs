using MediatR;
using System.ComponentModel.DataAnnotations;

namespace BankMore.Contas.Application.Commands.RegisterAccount;

/// <summary>
/// Comando para cadastrar uma nova conta corrente
/// </summary>
public class RegisterAccountCommand : IRequest<RegisterAccountResponse>
{
    /// <summary>
    /// CPF do usuário (11 dígitos, apenas números). Exemplo: "12345678909"
    /// </summary>
    /// <example>12345678909</example>
    [Required(ErrorMessage = "CPF é obrigatório")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "CPF deve ter exatamente 11 dígitos")]
    public string Cpf { get; set; } = string.Empty;

    /// <summary>
    /// Senha da conta (mínimo 6 caracteres)
    /// </summary>
    /// <example>senha123</example>
    [Required(ErrorMessage = "Senha é obrigatória")]
    [MinLength(6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Nome completo do titular da conta
    /// </summary>
    /// <example>João Silva</example>
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 200 caracteres")]
    public string Name { get; set; } = string.Empty;
}

