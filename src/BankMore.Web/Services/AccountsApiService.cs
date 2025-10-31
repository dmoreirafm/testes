using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace BankMore.Web.Services;

public class AccountsApiService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    public AccountsApiService(HttpClient httpClient, AuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public async Task<RegisterResponse?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Limpa headers de autorização para registro (não precisa autenticação)
            _httpClient.DefaultRequestHeaders.Authorization = null;
            
            var response = await _httpClient.PostAsJsonAsync("api/contas/cadastrar", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<RegisterResponse>();
            }
            
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            throw new Exception(error?.Message ?? "Erro ao registrar conta");
        }
        catch (TaskCanceledException)
        {
            throw new Exception("A requisição demorou muito para responder. Verifique se a API está rodando na porta 5001.");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Erro de conexão com a API: {ex.Message}. Verifique se a Accounts API está rodando.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao registrar: {ex.Message}", ex);
        }
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            // Limpa headers de autorização para login (ainda não tem token)
            _httpClient.DefaultRequestHeaders.Authorization = null;
            
            var response = await _httpClient.PostAsJsonAsync("api/contas/entrar", request);
            
            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (loginResponse != null)
                {
                    await _authService.SetTokenAsync(loginResponse.Token, loginResponse.AccountNumber);
                }
                return loginResponse;
            }
            
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            throw new Exception(error?.Message ?? "Erro ao fazer login");
        }
        catch (TaskCanceledException)
        {
            throw new Exception("A requisição demorou muito para responder. Verifique se a API está rodando na porta 5001.");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Erro de conexão com a API: {ex.Message}. Verifique se a Accounts API está rodando.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao fazer login: {ex.Message}", ex);
        }
    }

    public async Task<BalanceResponse?> GetBalanceAsync(string? accountNumber = null)
    {
        try
        {
            var token = await _authService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("Não autenticado. Faça login novamente.");
            }

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var url = string.IsNullOrEmpty(accountNumber) 
                ? "api/contas/saldo" 
                : $"api/contas/saldo?accountNumber={accountNumber}";

            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();
                return balanceResponse;
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new Exception("Sessão expirada. Faça login novamente.");
            }
            
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            throw new Exception(error?.Message ?? $"Erro ao consultar saldo. Status: {response.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            throw new Exception("A requisição demorou muito para responder. Verifique se a API está rodando na porta 5001.");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Erro de conexão com a API: {ex.Message}. Verifique se a Accounts API está rodando.");
        }
        catch (Exception ex)
        {
            // Re-throw sem adicionar mensagem extra se já for uma Exception criada por nós
            if (ex.Message.Contains("Não autenticado") || ex.Message.Contains("Sessão expirada") || ex.Message.Contains("demorou muito"))
            {
                throw;
            }
            throw new Exception($"Erro ao consultar saldo: {ex.Message}", ex);
        }
    }

    public async Task MakeTransactionAsync(TransactionRequest request)
    {
        try
        {
            var token = await _authService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("Não autenticado");
            }

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            request.RequestId = Guid.NewGuid().ToString();

            var response = await _httpClient.PostAsJsonAsync("api/contas/movimentacoes", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new Exception(error?.Message ?? "Erro ao realizar transação");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao realizar transação: {ex.Message}", ex);
        }
    }
}

