namespace BankMore.Contas.Domain.Common;

public class DomainException : Exception
{
    public string ErrorCode { get; }

    public DomainException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
}

