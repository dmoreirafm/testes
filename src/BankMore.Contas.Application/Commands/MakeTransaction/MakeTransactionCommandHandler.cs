using BankMore.Contas.Domain.Entities;
using BankMore.Contas.Domain.Enums;
using BankMore.Contas.Domain.Repositories;
using BankMore.Contas.Domain.ValueObjects;
using MediatR;

namespace BankMore.Contas.Application.Commands.MakeTransaction;

public class MakeTransactionCommandHandler : IRequestHandler<MakeTransactionCommand, MakeTransactionResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;

    public MakeTransactionCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<MakeTransactionResponse> Handle(MakeTransactionCommand request, CancellationToken cancellationToken)
    {
        // Valida idempotência
        var existingTransaction = await _transactionRepository.GetByRequestIdAsync(request.RequestId, cancellationToken);
        if (existingTransaction != null)
        {
            var existingAccount = await _accountRepository.GetByIdAsync(existingTransaction.AccountId, cancellationToken);
            if (existingAccount == null)
                throw new Domain.Common.DomainException("Conta não encontrada.", "INVALID_ACCOUNT");

            var balance = await _accountRepository.GetBalanceAsync(existingTransaction.AccountId, cancellationToken);

            return new MakeTransactionResponse
            {
                TransactionId = existingTransaction.Id.ToString(),
                AccountNumber = existingAccount.AccountNumber.Value,
                Amount = existingTransaction.Amount,
                Type = existingTransaction.Type == TransactionType.Credit ? "C" : "D",
                NewBalance = balance,
                CreatedAt = existingTransaction.CreatedAt
            };
        }

        // Valida tipo
        if (request.Type != 'C' && request.Type != 'D')
            throw new Domain.Common.DomainException("Tipo de transação inválido. Use 'C' para crédito ou 'D' para débito.", "INVALID_TYPE");

        var transactionType = request.Type == 'C' ? TransactionType.Credit : TransactionType.Debit;

        // Valida valor
        if (request.Amount <= 0)
            throw new Domain.Common.DomainException("Apenas valores positivos podem ser recebidos.", "INVALID_VALUE");

        // Obtém conta
        Account? account;
        if (!string.IsNullOrWhiteSpace(request.AccountNumber))
        {
            var accountNumber = AccountNumber.FromString(request.AccountNumber);
            account = await _accountRepository.GetByAccountNumberAsync(accountNumber, cancellationToken);
        }
        else
        {
            // Será injetado pelo middleware do token
            throw new Domain.Common.DomainException("Número da conta é obrigatório ou token inválido.", "INVALID_ACCOUNT");
        }

        if (account == null)
            throw new Domain.Common.DomainException("Conta não encontrada.", "INVALID_ACCOUNT");

        if (!account.IsActive())
            throw new Domain.Common.DomainException("Conta inativa.", "INACTIVE_ACCOUNT");

        // Validações de negócio
        // Apenas créditos podem ser feitos em contas diferentes da logada
        // (isso será validado no contexto do token no controller)

        // Valida saldo suficiente para débitos
        if (transactionType == TransactionType.Debit)
        {
            var currentBalance = await _accountRepository.GetBalanceAsync(account.Id, cancellationToken);
            if (currentBalance < request.Amount)
            {
                throw new Domain.Common.DomainException(
                    $"Saldo insuficiente. Saldo disponível: R$ {currentBalance:N2}. Valor necessário: R$ {request.Amount:N2}.",
                    "INSUFFICIENT_FUNDS");
            }
        }

        // Cria transação
        var transaction = Transaction.Create(account.Id, request.RequestId, request.Amount, transactionType);
        await _transactionRepository.CreateAsync(transaction, cancellationToken);

        var newBalance = await _accountRepository.GetBalanceAsync(account.Id, cancellationToken);

        return new MakeTransactionResponse
        {
            TransactionId = transaction.Id.ToString(),
            AccountNumber = account.AccountNumber.Value,
            Amount = transaction.Amount,
            Type = transaction.Type == TransactionType.Credit ? "C" : "D",
            NewBalance = newBalance,
            CreatedAt = transaction.CreatedAt
        };
    }
}

