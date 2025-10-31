using BankMore.Contas.Application.Services;
using BankMore.Contas.Domain.Repositories;
using MediatR;

namespace BankMore.Contas.Application.Commands.DeactivateAccount;

public class DeactivateAccountCommandHandler : IRequestHandler<DeactivateAccountCommand, DeactivateAccountResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;

    public DeactivateAccountCommandHandler(
        IAccountRepository accountRepository,
        IPasswordHasher passwordHasher)
    {
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
    }

    public Task<DeactivateAccountResponse> Handle(DeactivateAccountCommand request, CancellationToken cancellationToken)
    {
        // Este método será chamado com o accountId já obtido no controller
        return Task.FromException<DeactivateAccountResponse>(
            new InvalidOperationException("Use HandleWithAccountId ao invés de Handle."));
    }

    public async Task<DeactivateAccountResponse> HandleWithAccountId(int accountId, DeactivateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        
        if (account == null)
            throw new Domain.Common.DomainException("Conta não encontrada.", "INVALID_ACCOUNT");

        if (!account.IsActive())
            throw new Domain.Common.DomainException("Conta já está inativa.", "INACTIVE_ACCOUNT");

        // Valida senha
        if (!_passwordHasher.VerifyPassword(request.Password, account.PasswordHash))
            throw new Domain.Common.DomainException("Senha inválida.", "USER_UNAUTHORIZED");

        // Inativa conta
        account.Deactivate();
        await _accountRepository.UpdateAsync(account, cancellationToken);

        return new DeactivateAccountResponse
        {
            AccountNumber = account.AccountNumber.Value,
            Message = "Conta inativada com sucesso."
        };
    }
}


