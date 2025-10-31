using BankMore.Contas.Domain.Repositories;
using BankMore.Contas.Domain.ValueObjects;
using MediatR;

namespace BankMore.Contas.Application.Queries.GetBalance;

public class GetBalanceQueryHandler : IRequestHandler<GetBalanceQuery, GetBalanceResponse>
{
    private readonly IAccountRepository _accountRepository;

    public GetBalanceQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<GetBalanceResponse> Handle(GetBalanceQuery request, CancellationToken cancellationToken)
    {
        // Se não informado, será obtido do token
        if (string.IsNullOrWhiteSpace(request.AccountNumber))
            throw new Domain.Common.DomainException("Número da conta é obrigatório ou token inválido.", "INVALID_ACCOUNT");

        var accountNumber = AccountNumber.FromString(request.AccountNumber);
        var account = await _accountRepository.GetByAccountNumberAsync(accountNumber, cancellationToken);

        if (account == null)
            throw new Domain.Common.DomainException("Conta não encontrada.", "INVALID_ACCOUNT");

        if (!account.IsActive())
            throw new Domain.Common.DomainException("Conta inativa.", "INACTIVE_ACCOUNT");

        var balance = await _accountRepository.GetBalanceAsync(account.Id, cancellationToken);

        return new GetBalanceResponse
        {
            AccountNumber = account.AccountNumber.Value,
            AccountHolderName = account.Name,
            Balance = balance,
            ConsultedAt = DateTime.UtcNow
        };
    }
}

