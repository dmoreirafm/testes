using Blazored.LocalStorage;

namespace BankMore.Web.Services;

public class AuthService
{
    private readonly ILocalStorageService _localStorage;
    private const string TokenKey = "authToken";
    private const string AccountNumberKey = "accountNumber";

    public AuthService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task SetTokenAsync(string token, string accountNumber)
    {
        await _localStorage.SetItemAsync(TokenKey, token);
        await _localStorage.SetItemAsync(AccountNumberKey, accountNumber);
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _localStorage.GetItemAsync<string>(TokenKey);
    }

    public async Task<string?> GetAccountNumberAsync()
    {
        return await _localStorage.GetItemAsync<string>(AccountNumberKey);
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        await _localStorage.RemoveItemAsync(AccountNumberKey);
    }
}

