using BankMore.Contas.Application.Services;
using BankMore.Contas.Domain.Entities;
using BankMore.Contas.Domain.Repositories;
using BankMore.Contas.Domain.ValueObjects;
using MediatR;

namespace BankMore.Contas.Application.Commands.RegisterAccount;

public class RegisterAccountCommandHandler : IRequestHandler<RegisterAccountCommand, RegisterAccountResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterAccountCommandHandler(IAccountRepository accountRepository, IPasswordHasher passwordHasher)
    {
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterAccountResponse> Handle(RegisterAccountCommand request, CancellationToken cancellationToken)
    {
        var cpf = Cpf.Create(request.Cpf);

        // Verifica se já existe conta com este CPF
        var existingAccount = await _accountRepository.GetByCpfAsync(cpf, cancellationToken);
        if (existingAccount != null)
            throw new Domain.Common.DomainException("Já existe uma conta cadastrada com este CPF.", "DUPLICATE_ACCOUNT");

        // Gera número de conta único (com limite de tentativas para evitar loop infinito)
        AccountNumber accountNumber;
        Account? existingAccountByNumber;
        int attempts = 0;
        const int maxAttempts = 10;
        
        do
        {
            accountNumber = AccountNumber.Create();
            existingAccountByNumber = await _accountRepository.GetByAccountNumberAsync(accountNumber, cancellationToken);
            attempts++;
            
            if (attempts >= maxAttempts)
                throw new Domain.Common.DomainException(
                    "Não foi possível gerar um número de conta único após várias tentativas. Tente novamente.", 
                    "ACCOUNT_NUMBER_GENERATION_FAILED");
        } while (existingAccountByNumber != null);

        // Hash da senha
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Cria conta
        var account = Account.Create(cpf, accountNumber, request.Name, passwordHash);
        await _accountRepository.CreateAsync(account, cancellationToken);

        return new RegisterAccountResponse
        {
            AccountNumber = accountNumber.Value,
            Message = "Conta cadastrada com sucesso."
        };
    }
}

