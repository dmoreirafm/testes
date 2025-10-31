using BankMore.Transferencias.Application.Services;
//using KafkaFlow.Microsoft.DependencyInjection;
using BankMore.Transferencias.Domain.Repositories;
using BankMore.Transferencias.Infrastructure.Database;
using BankMore.Transferencias.Infrastructure.Repositories;
using BankMore.Transferencias.Infrastructure.Services;
using KafkaFlow;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Data.SQLite;

namespace BankMore.Transferencias.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=transfers.db;Version=3;";
        
        // Initialize database apenas uma vez (singleton)
        var initConnection = new SQLiteConnection(connectionString);
        initConnection.Open();
        var initializer = new DatabaseInitializer(initConnection);
        initializer.Initialize();
        initConnection.Close();
        initConnection.Dispose();
        
        // Factory para criar conexões scoped (uma por request)
        services.AddScoped<IDbConnection>(sp =>
        {
            var connString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Data Source=transfers.db;Version=3;";
            var connection = new SQLiteConnection(connString);
            connection.Open();
            return connection;
        });

        // Repositories
        services.AddScoped<ITransferRepository, TransferRepository>();

        // HTTP Client para Accounts API
        services.AddHttpClient<IAccountsApiClient, AccountsApiClient>(client =>
        {
            // No Docker, usa o nome do container; local usa localhost
            var baseUrl = configuration["AccountsApi:BaseUrl"] ?? "http://localhost:5001";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(60); // Aumentado para 60 segundos
        });

        // KafkaFlow Producer configurado
        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var transferRealizedTopic = configuration["Kafka:Topics:TransferRealized"] ?? "transferencias-realizadas";

        services.AddKafka(
            kafka => kafka
                //.UseConsoleLog()
                .AddCluster(
                    cluster => cluster
                        .WithBrokers(new[] { bootstrapServers })
                        .AddProducer(
                            "transfers-producer",
                            producer => producer
                                .DefaultTopic(transferRealizedTopic)))
                );

        // IMessageProducer será resolvido via KafkaFlow automaticamente quando necessário

        return services;
    }
}

