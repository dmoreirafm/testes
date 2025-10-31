using BankMore.Contas.Application.Services;
using BankMore.Contas.Domain.Repositories;
using MediatR;

namespace BankMore.Contas.Application.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IAccountRepository accountRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByCpfOrAccountNumberAsync(request.Login, cancellationToken);
        
        if (account == null)
            throw new Domain.Common.DomainException("CPF/Número de conta ou senha inválidos.", "USER_UNAUTHORIZED");

        if (!account.IsActive())
            throw new Domain.Common.DomainException("Conta inativa.", "INACTIVE_ACCOUNT");

        if (!_passwordHasher.VerifyPassword(request.Password, account.PasswordHash))
            throw new Domain.Common.DomainException("CPF/Número de conta ou senha inválidos.", "USER_UNAUTHORIZED");

        var token = _tokenService.GenerateToken(account.Id, account.AccountNumber.Value);

        return new LoginResponse
        {
            Token = token.Token,
            AccountId = account.Id.ToString(),
            AccountNumber = account.AccountNumber.Value,
            ExpiresAt = token.ExpiresAt
        };
    }
}

