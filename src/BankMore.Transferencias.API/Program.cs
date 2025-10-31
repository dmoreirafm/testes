using BankMore.Contas.Application.Services;
using BankMore.Transferencias.API.Middleware;
using BankMore.Transferencias.Application.Services;
using BankMore.Transferencias.Infrastructure;
using KafkaFlow;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

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

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BankMore Transfers API",
        Version = "v1",
        Description = "API para transferências entre contas da BankMore. " +
                      "Permite transferir valores entre contas correntes da mesma instituição " +
                      "com compensação automática em caso de falhas.",
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
    
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

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

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(BankMore.Transferencias.Application.Commands.CreateTransfer.CreateTransferCommand).Assembly));

builder.Services.AddInfrastructure(builder.Configuration);

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

builder.Services.AddSingleton<ITokenService>(sp =>
{
    return new BankMore.Transferencias.Infrastructure.Services.TokenService(builder.Configuration);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazorApp");
app.UseJwtMiddleware();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

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
