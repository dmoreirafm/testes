using BankMore.Contas.API.Middleware;
using BankMore.Contas.Infrastructure;
using KafkaFlow;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp", policy =>
    {
        policy.WithOrigins("http://localhost:5000", "https://localhost:5005", "http://web:8080", "http://bankmore-web:8080")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BankMore Accounts API",
        Version = "v1",
        Description = "API para gerenciamento de contas correntes da BankMore. " +
                      "Esta API permite cadastrar contas, realizar login, consultar saldo, " +
                      "fazer movimentações (depósitos e saques) e inativar contas.",
        Contact = new OpenApiContact
        {
            Name = "BankMore",
            Email = "contato@bankmore.com.br"
        },
        License = new OpenApiLicense
        {
            Name = "Proprietary",
            Url = new Uri("https://bankmore.com.br/license")
        }
    });
    
    // Inclui XML comments dos DTOs e Controllers
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Adiciona suporte para JWT no Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando o esquema Bearer. Exemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(BankMore.Contas.Application.Commands.RegisterAccount.RegisterAccountCommand).Assembly));

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddKafkaConsumers(builder.Configuration);

// JWT Authentication
var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "BankMore_SecretKey_Minimum_32_Characters_Long_For_HS256";
var issuer = builder.Configuration["Jwt:Issuer"] ?? "BankMore";
var audience = builder.Configuration["Jwt:Audience"] ?? "BankMore";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS deve vir antes de Authentication
app.UseCors("AllowBlazorApp");

// Routing
app.UseRouting();

// JWT Middleware (deve vir antes de UseAuthentication)
app.UseJwtMiddleware();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Inicia KafkaBus
try
{
    var kafkaBus = app.Services.CreateKafkaBus();
    await kafkaBus.StartAsync();
    Console.WriteLine("KafkaBus iniciado com sucesso!");
}
catch (Exception ex)
{
    Console.WriteLine($"Erro ao iniciar KafkaBus: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

app.Run();
