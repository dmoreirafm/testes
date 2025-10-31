using BankMore.Contas.Domain.Entities;
using BankMore.Contas.Domain.Enums;
using BankMore.Contas.Domain.ValueObjects;
using FluentAssertions;

namespace BankMore.Contas.Tests;

public class AccountTests
{
    [Fact]
    public void Account_Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var cpf = Cpf.Create("12345678909"); // CPF válido
        var accountNumber = AccountNumber.Create("1234567890");
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("senha123");

        // Act
        var account = Account.Create(cpf, accountNumber, "João Silva", passwordHash);

        // Assert
        account.Should().NotBeNull();
        account.Cpf.Value.Should().Be("12345678909");
        account.Name.Should().Be("João Silva");
        account.PasswordHash.Should().NotBeNullOrEmpty();
        account.Status.Should().Be(AccountStatus.Active);
    }

    [Fact]
    public void Account_Create_WithInvalidName_ShouldThrowException()
    {
        // Arrange
        var cpf = Cpf.Create("12345678909");
        var accountNumber = AccountNumber.Create("1234567890");
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("senha123");

        // Act & Assert
        Assert.Throws<Domain.Common.DomainException>(() => 
            Account.Create(cpf, accountNumber, "", passwordHash));
    }

    [Fact]
    public void Account_Deactivate_ShouldChangeStatusToInactive()
    {
        // Arrange
        var cpf = Cpf.Create("12345678909");
        var accountNumber = AccountNumber.Create("1234567890");
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("senha123");
        var account = Account.Create(cpf, accountNumber, "João Silva", passwordHash);

        // Act
        account.Deactivate();

        // Assert
        account.Status.Should().Be(AccountStatus.Inactive);
    }

    [Fact]
    public void Cpf_Create_WithInvalidCpf_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<Domain.Common.DomainException>(() => Cpf.Create("123"));
    }
}
