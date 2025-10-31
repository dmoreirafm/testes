using BankMore.Contas.Domain.Common;

namespace BankMore.Contas.Domain.ValueObjects;

public class Cpf
{
    public string Value { get; private set; }

    private Cpf(string value)
    {
        Value = value;
    }

    public static Cpf Create(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            throw new DomainException("CPF não pode ser vazio.", "INVALID_DOCUMENT");

        // Remove caracteres não numéricos
        var digits = new string(cpf.Where(char.IsDigit).ToArray());

        if (digits.Length < 11)
            throw new DomainException("CPF deve conter pelo menos 11 dígitos numéricos.", "INVALID_DOCUMENT");

        if (digits.Length > 11)
            throw new DomainException("CPF deve conter exatamente 11 dígitos numéricos.", "INVALID_DOCUMENT");

        // Validação completa do algoritmo de CPF
        if (!IsValidCpf(digits))
            throw new DomainException("CPF inválido.", "INVALID_DOCUMENT");

        return new Cpf(digits);
    }

    private static bool IsValidCpf(string cpf)
    {
        if (cpf.Length != 11)
            return false;

        // Verifica se todos os dígitos são iguais
        if (cpf.All(c => c == cpf[0]))
            return false;

        // Valida primeiro dígito verificador
        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += int.Parse(cpf[i].ToString()) * (10 - i);

        int remainder = sum % 11;
        int digit1 = remainder < 2 ? 0 : 11 - remainder;

        if (digit1 != int.Parse(cpf[9].ToString()))
            return false;

        // Valida segundo dígito verificador
        sum = 0;
        for (int i = 0; i < 10; i++)
            sum += int.Parse(cpf[i].ToString()) * (11 - i);

        remainder = sum % 11;
        int digit2 = remainder < 2 ? 0 : 11 - remainder;

        return digit2 == int.Parse(cpf[10].ToString());
    }

    public override string ToString() => Value;
}

