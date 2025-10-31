using MediatR;

namespace BankMore.Contas.Application.Queries.GetBalance;

public class GetBalanceQuery : IRequest<GetBalanceResponse>
{
    public string? AccountNumber { get; set; }
}

