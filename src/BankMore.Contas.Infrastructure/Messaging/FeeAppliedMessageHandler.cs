using BankMore.Contas.Domain.Entities;
using BankMore.Contas.Domain.Enums;
using BankMore.Contas.Domain.Messages;
using BankMore.Contas.Domain.Repositories;
using BankMore.Contas.Domain.ValueObjects;
using KafkaFlow;
using KafkaFlow.Consumers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BankMore.Contas.Infrastructure.Messaging;

public class FeeAppliedMessageHandler : IMessageMiddleware
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<FeeAppliedMessageHandler> _logger;

    public FeeAppliedMessageHandler(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<FeeAppliedMessageHandler> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task Invoke(IMessageContext context, MiddlewareDelegate next)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var transactionRepository = scope.ServiceProvider.GetRequiredService<Domain.Repositories.ITransactionRepository>();

        try
        {
            // Extrai a mensagem do contexto
            string messageJson;
            var messageObj = context.Message;
            if (messageObj is string str)
            {
                messageJson = str;
            }
            else if (messageObj is byte[] bytes)
            {
                messageJson = System.Text.Encoding.UTF8.GetString(bytes);
            }
            else
            {
                messageJson = messageObj?.ToString() ?? string.Empty;
            }
            
            if (string.IsNullOrEmpty(messageJson))
            {
                _logger.LogWarning("Mensagem de tarifa recebida é vazia.");
                return;
            }

            var message = JsonSerializer.Deserialize<FeeAppliedMessage>(messageJson);
            if (message == null)
            {
                _logger.LogWarning("Mensagem de tarifa recebida é nula ou inválida.");
                return;
            }

            _logger.LogInformation(
                "Processando débito de tarifa de {FeeAmount} na conta {AccountNumber}",
                message.FeeAmount, message.AccountNumber);

            // Obtém a conta
            var accountNumber = AccountNumber.FromString(message.AccountNumber);
            var account = await accountRepository.GetByAccountNumberAsync(accountNumber);

            if (account == null)
            {
                _logger.LogWarning(
                    "Conta {AccountNumber} não encontrada para débito de tarifa",
                    message.AccountNumber);
                return;
            }

            if (!account.IsActive())
            {
                _logger.LogWarning(
                    "Conta {AccountNumber} inativa, não é possível debitar tarifa",
                    message.AccountNumber);
                return;
            }

            // Cria transação de débito da tarifa
            var requestId = $"FEE-{Guid.NewGuid()}"; // RequestId único para a tarifa
            var transaction = Transaction.Create(
                account.Id,
                requestId,
                message.FeeAmount,
                TransactionType.Debit);

            await transactionRepository.CreateAsync(transaction);

            _logger.LogInformation(
                "Tarifa de {FeeAmount} debitada com sucesso da conta {AccountNumber}",
                message.FeeAmount, message.AccountNumber);

            // Chama o próximo middleware no pipeline (se houver)
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado no consumer de tarifas.");
            throw; // KafkaFlow irá tratar o erro e fazer retry se configurado
        }
    }
}

