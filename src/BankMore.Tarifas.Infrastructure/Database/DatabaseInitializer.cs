using System.Data;
using Dapper;

namespace BankMore.Tarifas.Infrastructure.Database;

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
        const string feesTable = @"
            CREATE TABLE IF NOT EXISTS Fees (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TransferId TEXT NOT NULL UNIQUE,
                AccountNumber TEXT NOT NULL,
                TransferAmount DECIMAL(18,2) NOT NULL,
                FeeAmount DECIMAL(18,2) NOT NULL,
                AppliedAt DATETIME NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Fees_TransferId ON Fees(TransferId);
            CREATE INDEX IF NOT EXISTS IX_Fees_AccountNumber ON Fees(AccountNumber);
        ";

        _connection.Execute(feesTable);
    }
}

