using BankMore.Contas.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BankMore.Transferencias.Infrastructure.Services;

// Nota: Em produção, este serviço deveria ser compartilhado via biblioteca comum
// Por enquanto, duplicamos a lógica para manter os microsserviços independentes
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TokenResult GenerateToken(int accountId, string accountNumber)
    {
        // Não usado neste microsserviço, mas implementado para compatibilidade
        throw new NotImplementedException("Geração de token é feita pela Accounts API");
    }

    public bool ValidateToken(string token, out int accountId, out string accountNumber)
    {
        accountId = 0;
        accountNumber = string.Empty;

        try
        {
            var secretKey = _configuration["Jwt:SecretKey"] ?? "BankMore_SecretKey_Minimum_32_Characters_Long_For_HS256";
            var issuer = _configuration["Jwt:Issuer"] ?? "BankMore";
            var audience = _configuration["Jwt:Audience"] ?? "BankMore";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            
            var accountIdClaim = principal.FindFirst("accountId")?.Value;
            var accountNumberClaim = principal.FindFirst("accountNumber")?.Value;

            if (accountIdClaim != null && accountNumberClaim != null)
            {
                accountId = int.Parse(accountIdClaim);
                accountNumber = accountNumberClaim;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}

