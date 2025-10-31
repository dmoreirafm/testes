using BankMore.Web.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddBlazoredLocalStorage();

// Configuração das APIs
var accountsApiUrl = builder.Configuration["ApiSettings:AccountsApi"] ?? "http://localhost:5001";
var transfersApiUrl = builder.Configuration["ApiSettings:TransfersApi"] ?? "http://localhost:5002";
var feesApiUrl = builder.Configuration["ApiSettings:FeesApi"] ?? "http://localhost:5003";

// HttpClient para Accounts API
builder.Services.AddHttpClient<AccountsApiService>(client =>
{
    client.BaseAddress = new Uri(accountsApiUrl);
    client.Timeout = TimeSpan.FromSeconds(60); // Aumentado para 60 segundos
});

// HttpClient para Transfers API
builder.Services.AddHttpClient<TransfersApiService>(client =>
{
    client.BaseAddress = new Uri(transfersApiUrl);
    client.Timeout = TimeSpan.FromSeconds(60); // Aumentado para 60 segundos
});

// HttpClient para Fees API
builder.Services.AddHttpClient<FeesApiService>(client =>
{
    client.BaseAddress = new Uri(feesApiUrl);
    client.Timeout = TimeSpan.FromSeconds(60); // Aumentado para 60 segundos
});

// Services
// Nota: AccountsApiService, TransfersApiService e FeesApiService já são registrados pelo AddHttpClient acima
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
