using BankMore.Tarifas.Infrastructure;
using KafkaFlow;
using KafkaFlow.Microsoft.DependencyInjection;
using Microsoft.OpenApi.Models;

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
        Title = "BankMore Fees API",
        Version = "v1",
        Description = "API para gerenciamento de tarifas da BankMore. " +
                      "Esta API gerencia as tarifas aplicadas automaticamente em transferências. " +
                      "As tarifas são aplicadas de forma assíncrona via Kafka quando transferências são realizadas.",
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
});

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddKafkaConsumers(builder.Configuration);

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
