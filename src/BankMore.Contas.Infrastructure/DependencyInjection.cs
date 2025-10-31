using BankMore.Contas.Application.Services;
//using KafkaFlow.Microsoft.DependencyInjection;
using BankMore.Contas.Domain.Repositories;
using BankMore.Contas.Infrastructure.Database;
using BankMore.Contas.Infrastructure.Messaging;
using BankMore.Contas.Infrastructure.Repositories;
using BankMore.Contas.Infrastructure.Services;
using KafkaFlow;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data;
using System.Data.SQLite;

namespace BankMore.Contas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=accounts.db;Version=3;";
        
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
                ?? "Data Source=accounts.db;Version=3;";
            var connection = new SQLiteConnection(connString);
            connection.Open();
            return connection;
        });

        // Repositories
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<Domain.Repositories.ITransactionRepository, TransactionRepository>();

        // Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }

    public static IServiceCollection AddKafkaConsumers(this IServiceCollection services, IConfiguration configuration)
    {
        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var feeAppliedTopic = configuration["Kafka:Topics:FeeApplied"] ?? "tarifacoes-realizadas";

        services.AddKafka(
            kafka => kafka
                //.UseConsoleLog()
                .AddCluster(
                    cluster => cluster
                        .WithBrokers(new[] { bootstrapServers })
                        .AddConsumer(
                            consumer => consumer
                                .Topic(feeAppliedTopic)
                                .WithGroupId("accounts-consumer-group")
                                .WithBufferSize(100)
                                .WithWorkersCount(1)
                                .AddMiddlewares(
                                    middlewares => middlewares
                                        .Add<FeeAppliedMessageHandler>()))
                ));

        return services;
    }
}

