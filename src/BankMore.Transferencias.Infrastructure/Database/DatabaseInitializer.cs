using System.Data;
using Dapper;

namespace BankMore.Transferencias.Infrastructure.Database;

public class DatabaseInitializer
{
    private readonly IDbConnection _connection;

    public DatabaseInitializer(IDbConnection connection)
    {
        _connection = connection;
    }

    public void Initialize()
    {
        CreateTables();
    }

    private void CreateTables()
    {
        const string transfersTable = @"
            CREATE TABLE IF NOT EXISTS Transfers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RequestId TEXT NOT NULL UNIQUE,
                OriginAccountNumber TEXT NOT NULL,
                DestinationAccountNumber TEXT NOT NULL,
                Amount DECIMAL(18,2) NOT NULL,
                Status INTEGER NOT NULL DEFAULT 1,
                FailureReason TEXT,
                CreatedAt DATETIME NOT NULL,
                UpdatedAt DATETIME
            );

            CREATE INDEX IF NOT EXISTS IX_Transfers_RequestId ON Transfers(RequestId);
            CREATE INDEX IF NOT EXISTS IX_Transfers_OriginAccountNumber ON Transfers(OriginAccountNumber);
            CREATE INDEX IF NOT EXISTS IX_Transfers_DestinationAccountNumber ON Transfers(DestinationAccountNumber);
        ";

        _connection.Execute(transfersTable);
    }
}

