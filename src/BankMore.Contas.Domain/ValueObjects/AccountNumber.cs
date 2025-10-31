using BankMore.Contas.Domain.Common;

namespace BankMore.Contas.Domain.ValueObjects;

public class AccountNumber
{
    public string Value { get; private set; }

    private AccountNumber(string value)
    {
        Value = value;
    }

    public static AccountNumber Create(string? accountNumber = null)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            // Gera número de conta aleatório de 10 dígitos
            var random = new Random();
            // Usa Next para gerar número entre 0 e 999999999, depois adiciona zeros à esquerda
            var number = random.Next(0, 1000000000);
            accountNumber = number.ToString("D10");
        }

        if (accountNumber.Length != 10 || !accountNumber.All(char.IsDigit))
            throw new DomainException("Número de conta inválido. Deve ter 10 dígitos numéricos.", "INVALID_ACCOUNT");

        return new AccountNumber(accountNumber);
    }

    public static AccountNumber FromString(string accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new DomainException("Número de conta não pode ser vazio.", "INVALID_ACCOUNT");

        if (accountNumber.Length != 10 || !accountNumber.All(char.IsDigit))
            throw new DomainException("Número de conta inválido.", "INVALID_ACCOUNT");

        return new AccountNumber(accountNumber);
    }

    public override string ToString() => Value;
}

