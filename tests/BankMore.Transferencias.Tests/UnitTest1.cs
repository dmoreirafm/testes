using BankMore.Transferencias.Domain.Entities;
using BankMore.Transferencias.Domain.Enums;
using FluentAssertions;

namespace BankMore.Transferencias.Tests;

public class TransferTests
{
    [Fact]
    public void Transfer_Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();
        var originAccount = "1234567890";
        var destinationAccount = "0987654321";
        var amount = 100.00m;

        // Act
        var transfer = Transfer.Create(requestId, originAccount, destinationAccount, amount);

        // Assert
        transfer.Should().NotBeNull();
        transfer.RequestId.Should().Be(requestId);
        transfer.OriginAccountNumber.Should().Be(originAccount);
        transfer.DestinationAccountNumber.Should().Be(destinationAccount);
        transfer.Amount.Should().Be(amount);
        transfer.Status.Should().Be(TransferStatus.Pending);
    }

    [Fact]
    public void Transfer_Create_WithSameOriginAndDestination_ShouldThrowException()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();
        var accountNumber = "1234567890";
        var amount = 100.00m;

        // Act & Assert
        Assert.Throws<Domain.Common.DomainException>(() => 
            Transfer.Create(requestId, accountNumber, accountNumber, amount));
    }

    [Fact]
    public void Transfer_Create_WithZeroAmount_ShouldThrowException()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();
        var originAccount = "1234567890";
        var destinationAccount = "0987654321";

        // Act & Assert
        Assert.Throws<Domain.Common.DomainException>(() => 
            Transfer.Create(requestId, originAccount, destinationAccount, 0));
    }

    [Fact]
    public void Transfer_MarkAsCompleted_ShouldChangeStatus()
    {
        // Arrange
        var transfer = Transfer.Create(
            Guid.NewGuid().ToString(), 
            "1234567890", 
            "0987654321", 
            100.00m);

        // Act
        transfer.MarkAsCompleted();

        // Assert
        transfer.Status.Should().Be(TransferStatus.Completed);
    }
}
