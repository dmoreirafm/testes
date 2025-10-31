using System.Net.Http.Json;

namespace BankMore.Web.Services;

public class TransfersApiService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    public TransfersApiService(HttpClient httpClient, AuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public async Task CreateTransferAsync(TransferRequest request)
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

            request.RequestId = Guid.NewGuid().ToString();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var response = await _httpClient.PostAsJsonAsync("api/transferencias", request, cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                // 204 No Content é sucesso
                return;
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new Exception("Sessão expirada. Faça login novamente.");
            }
            
            var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cts.Token);
            throw new Exception(error?.Message ?? $"Erro ao realizar transferência. Status: {response.StatusCode}. Response: {errorContent}");
        }
        catch (TaskCanceledException)
        {
            throw new Exception("A requisição demorou muito para responder (timeout de 60s). Verifique se a Transfers API está rodando na porta 5002.");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Erro de conexão com a Transfers API: {ex.Message}. Verifique se a API está rodando.");
        }
        catch (Exception ex)
        {
            // Re-throw sem adicionar mensagem extra se já for uma Exception criada por nós
            if (ex.Message.Contains("Não autenticado") || ex.Message.Contains("Sessão expirada") || ex.Message.Contains("demorou muito"))
            {
                throw;
            }
            throw new Exception($"Erro ao realizar transferência: {ex.Message}", ex);
        }
    }
}

