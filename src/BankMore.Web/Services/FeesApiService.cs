using System.Net.Http.Json;

namespace BankMore.Web.Services;

public class FeesApiService
{
    private readonly HttpClient _httpClient;

    public FeesApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<FeeResponse>> GetFeesByAccountAsync(string accountNumber)
    {
        try
        {
            if (string.IsNullOrEmpty(accountNumber))
            {
                throw new Exception("Número da conta não informado.");
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = await _httpClient.GetAsync($"api/tarifas/conta/{accountNumber}", cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                var fees = await response.Content.ReadFromJsonAsync<IEnumerable<FeeResponse>>(cts.Token);
                return fees ?? Enumerable.Empty<FeeResponse>();
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // 404 é OK - significa que não há tarifas para essa conta
                return Enumerable.Empty<FeeResponse>();
            }
            
            var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cts.Token);
            throw new Exception(error?.Message ?? $"Erro ao consultar tarifas. Status: {response.StatusCode}. Response: {errorContent}");
        }
        catch (TaskCanceledException)
        {
            throw new Exception("A requisição demorou muito para responder (timeout). Verifique se a Fees API está rodando na porta 5003.");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Erro de conexão com a Fees API: {ex.Message}. Verifique se a API está rodando.");
        }
        catch (Exception ex)
        {
            // Re-throw com mensagem mais clara
            if (ex.Message.Contains("timeout") || ex.Message.Contains("demorou muito"))
            {
                throw;
            }
            throw new Exception($"Erro ao consultar tarifas: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<FeeResponse>> GetAllFeesAsync(int skip = 0, int take = 100)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/tarifas?skip={skip}&take={take}");
            
            if (response.IsSuccessStatusCode)
            {
                var fees = await response.Content.ReadFromJsonAsync<IEnumerable<FeeResponse>>();
                return fees ?? Enumerable.Empty<FeeResponse>();
            }
            
            return Enumerable.Empty<FeeResponse>();
        }
        catch
        {
            return Enumerable.Empty<FeeResponse>();
        }
    }
}

