using BankMore.Tarifas.Domain.Repositories;
using BankMore.Tarifas.Infrastructure.Database;
using BankMore.Tarifas.Infrastructure.Messaging;
using BankMore.Tarifas.Infrastructure.Repositories;
using KafkaFlow;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data;
using System.Data.SQLite;

namespace BankMore.Tarifas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=fees.db;Version=3;";
        
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
                ?? "Data Source=fees.db;Version=3;";
            var connection = new SQLiteConnection(connString);
            connection.Open();
            return connection;
        });

        // Repositories
        services.AddScoped<IFeeRepository, FeeRepository>();

        return services;
    }

    public static IServiceCollection AddKafkaConsumers(this IServiceCollection services, IConfiguration configuration)
    {
        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var transferRealizedTopic = configuration["Kafka:Topics:TransferRealized"] ?? "transferencias-realizadas";

        services.AddKafka(
            kafka => kafka
                .AddCluster(
                    cluster => cluster
                        .WithBrokers(new[] { bootstrapServers })
                        .AddProducer(
                            "fees-producer",
                            producer => producer
                                .DefaultTopic(configuration["Kafka:Topics:FeeApplied"] ?? "tarifacoes-realizadas"))
                        .AddConsumer(
                            consumer => consumer
                                .Topic(transferRealizedTopic)
                                .WithGroupId("fees-consumer-group")
                                .WithBufferSize(100)
                                .WithWorkersCount(1)
                                .AddMiddlewares(
                                    middlewares => middlewares
                                        .Add<TransferRealizedMessageHandler>())))
                );

        return services;
    }
}
