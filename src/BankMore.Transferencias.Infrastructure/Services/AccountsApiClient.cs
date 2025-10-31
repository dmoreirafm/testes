using BankMore.Transferencias.Application.Services;
using BankMore.Transferencias.Domain.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace BankMore.Transferencias.Infrastructure.Services;

// Modelo para deserializar erros da Accounts API
internal class ErrorResponse
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class AccountsApiClient : IAccountsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AccountsApiClient> _logger;
    private readonly string _accountsApiBaseUrl;

    public AccountsApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<AccountsApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _accountsApiBaseUrl = configuration["AccountsApi:BaseUrl"] ?? "http://localhost:5001";
        _httpClient.BaseAddress = new Uri(_accountsApiBaseUrl);
    }

    public async Task<MakeTransactionResponse> MakeDebitAsync(string requestId, string accountNumber, decimal amount, string jwtToken, CancellationToken cancellationToken = default)
    {
        var request = new MakeTransactionRequest
        {
            RequestId = requestId,
            AccountNumber = accountNumber,
            Amount = amount,
            Type = 'D' // Débito
        };

        return await MakeTransactionAsync(request, jwtToken, cancellationToken);
    }

    public async Task<MakeTransactionResponse> MakeCreditAsync(string requestId, string accountNumber, decimal amount, string jwtToken, CancellationToken cancellationToken = default)
    {
        var request = new MakeTransactionRequest
        {
            RequestId = requestId,
            AccountNumber = accountNumber,
            Amount = amount,
            Type = 'C' // Crédito
        };

        return await MakeTransactionAsync(request, jwtToken, cancellationToken);
    }

    public async Task<GetBalanceResponse> GetBalanceAsync(string accountNumber, string jwtToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Consultando saldo da conta {AccountNumber} para validação", accountNumber);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"/api/contas/saldo?accountNumber={accountNumber}");
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var balanceResponse = await response.Content.ReadFromJsonAsync<GetBalanceResponse>(cancellationToken: cancellationToken);
                if (balanceResponse == null)
                    throw new Domain.Common.DomainException("Resposta inválida da API de contas ao consultar saldo.", "INVALID_RESPONSE");

                return balanceResponse;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Erro ao consultar saldo. Status: {Status}, Response: {Response}", 
                    response.StatusCode, errorContent);

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    try
                    {
                        var errorResponse = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(errorContent);
                        if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Message))
                        {
                            throw new Domain.Common.DomainException(
                                errorResponse.Message,
                                errorResponse.ErrorCode);
                        }
                    }
                    catch (Domain.Common.DomainException)
                    {
                        throw;
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        throw new Domain.Common.DomainException(
                            $"Erro ao validar conta: {errorContent}",
                            "INVALID_ACCOUNT");
                    }
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    throw new Domain.Common.DomainException(
                        "Token inválido ou expirado.",
                        "USER_UNAUTHORIZED");
                }

                throw new Domain.Common.DomainException(
                    $"Falha ao validar conta. Status: {response.StatusCode}",
                    "ACCOUNTS_API_UNAVAILABLE");
            }
        }
        catch (Domain.Common.DomainException)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("Timeout ao consultar saldo da conta {AccountNumber}", accountNumber);
            throw new Domain.Common.DomainException(
                "Timeout ao validar conta. A requisição demorou mais de 30 segundos.",
                "ACCOUNTS_API_TIMEOUT");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar saldo da conta {AccountNumber}", accountNumber);
            throw new Domain.Common.DomainException(
                $"Erro ao validar conta: {ex.Message}",
                "ACCOUNTS_API_ERROR");
        }
    }

    private async Task<MakeTransactionResponse> MakeTransactionAsync(MakeTransactionRequest request, string jwtToken, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Chamando Accounts API: {Type} de {Amount} na conta {AccountNumber}", 
                request.Type, request.Amount, request.AccountNumber);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/contas/movimentacoes");
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            requestMessage.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                // 204 No Content é sucesso para transações
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation("Transação {Type} realizada com sucesso na conta {AccountNumber}", 
                        request.Type, request.AccountNumber);
                    // Retorna resposta vazia mas válida
                    return new MakeTransactionResponse
                    {
                        AccountNumber = request.AccountNumber ?? string.Empty,
                        Amount = request.Amount,
                        Type = request.Type.ToString(),
                        TransactionId = request.RequestId,
                        CreatedAt = DateTime.UtcNow
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<MakeTransactionResponse>(cancellationToken: cancellationToken);
                if (result == null)
                    throw new Domain.Common.DomainException("Resposta inválida da API de contas.", "INVALID_RESPONSE");

                _logger.LogInformation("Transação {Type} realizada com sucesso. Novo saldo: {NewBalance}", 
                    request.Type, result.NewBalance);

                return result;
            }
            else
            {
                _logger.LogError("Erro ao chamar API de contas. Status: {Status}", response.StatusCode);

                // Lê o conteúdo apenas uma vez
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    // Tenta deserializar o ErrorResponse da Accounts API
                    try
                    {
                        var errorResponse = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(errorContent);
                        if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Message))
                        {
                            _logger.LogWarning("Erro retornado pela Accounts API: {ErrorCode} - {Message}", 
                                errorResponse.ErrorCode, errorResponse.Message);
                            
                            // Se for erro de saldo insuficiente, propaga a mensagem original
                            if (errorResponse.ErrorCode == "INSUFFICIENT_FUNDS")
                            {
                                throw new Domain.Common.DomainException(
                                    errorResponse.Message,
                                    "INSUFFICIENT_FUNDS");
                            }
                            
                            throw new Domain.Common.DomainException(
                                errorResponse.Message,
                                errorResponse.ErrorCode);
                        }
                    }
                    catch (Domain.Common.DomainException)
                    {
                        throw; // Re-throw DomainException para propagar corretamente
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        // Se não conseguir deserializar, usa o conteúdo como string
                        _logger.LogWarning(ex, "Não foi possível deserializar erro da Accounts API. Conteúdo: {Content}", errorContent);
                        
                        throw new Domain.Common.DomainException(
                            $"Erro na API de contas: {errorContent}",
                            "ACCOUNTS_API_ERROR");
                    }
                }

                throw new Domain.Common.DomainException(
                    $"Falha na comunicação com API de contas. Status: {response.StatusCode}. Response: {errorContent}",
                    "ACCOUNTS_API_UNAVAILABLE");
            }
        }
        catch (Domain.Common.DomainException)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("Timeout ao chamar API de contas para transação {Type} na conta {AccountNumber}", 
                request.Type, request.AccountNumber);
            throw new Domain.Common.DomainException(
                $"Timeout ao comunicar com API de contas. A requisição demorou mais de 30 segundos.",
                "ACCOUNTS_API_TIMEOUT");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao chamar API de contas");
            throw new Domain.Common.DomainException(
                $"Erro ao comunicar com API de contas: {ex.Message}",
                "ACCOUNTS_API_ERROR");
        }
    }
}

