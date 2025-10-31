using System.Data;
using Dapper;

namespace BankMore.Contas.Infrastructure.Database;

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
        const string accountsTable = @"
            CREATE TABLE IF NOT EXISTS Accounts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Cpf TEXT NOT NULL UNIQUE,
                AccountNumber TEXT NOT NULL UNIQUE,
                Name TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                Status INTEGER NOT NULL DEFAULT 1,
                CreatedAt DATETIME NOT NULL,
                UpdatedAt DATETIME
            );

            CREATE INDEX IF NOT EXISTS IX_Accounts_Cpf ON Accounts(Cpf);
            CREATE INDEX IF NOT EXISTS IX_Accounts_AccountNumber ON Accounts(AccountNumber);
        ";

        const string transactionsTable = @"
            CREATE TABLE IF NOT EXISTS Transactions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AccountId INTEGER NOT NULL,
                RequestId TEXT NOT NULL UNIQUE,
                Amount DECIMAL(18,2) NOT NULL,
                Type TEXT NOT NULL,
                CreatedAt DATETIME NOT NULL,
                FOREIGN KEY (AccountId) REFERENCES Accounts(Id)
            );

            CREATE INDEX IF NOT EXISTS IX_Transactions_AccountId ON Transactions(AccountId);
            CREATE INDEX IF NOT EXISTS IX_Transactions_RequestId ON Transactions(RequestId);
        ";

        _connection.Execute(accountsTable);
        _connection.Execute(transactionsTable);
    }
}

