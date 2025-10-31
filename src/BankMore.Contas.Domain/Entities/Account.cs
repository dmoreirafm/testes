using BankMore.Contas.Domain.Enums;
using BankMore.Contas.Domain.ValueObjects;

namespace BankMore.Contas.Domain.Entities;

public class Account
{
    public int Id { get; set; }
    public Cpf Cpf { get; private set; }
    public AccountNumber AccountNumber { get; private set; }
    public string Name { get; private set; }
    public string PasswordHash { get; private set; }
    public AccountStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    private Account() { } // Para Dapper

    private Account(Cpf cpf, AccountNumber accountNumber, string name, string passwordHash)
    {
        Cpf = cpf;
        AccountNumber = accountNumber;
        Name = name;
        PasswordHash = passwordHash;
        Status = AccountStatus.Active;
        CreatedAt = DateTime.UtcNow;
    }

    public static Account Create(Cpf cpf, AccountNumber accountNumber, string name, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new Common.DomainException("Nome não pode ser vazio.", "INVALID_NAME");

        return new Account(cpf, accountNumber, name, passwordHash);
    }

    public Account WithId(int id)
    {
        var account = new Account(Cpf, AccountNumber, Name, PasswordHash);
        account.Id = id;
        account.Status = Status;
        account.CreatedAt = CreatedAt;
        account.UpdatedAt = UpdatedAt;
        return account;
    }

    public void Deactivate()
    {
        if (Status == AccountStatus.Inactive)
            throw new Common.DomainException("Conta já está inativa.", "INACTIVE_ACCOUNT");

        Status = AccountStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsActive() => Status == AccountStatus.Active;
}

