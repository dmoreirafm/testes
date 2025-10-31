using BankMore.Tarifas.Domain.Entities;
using BankMore.Tarifas.Domain.Messages;
using BankMore.Tarifas.Domain.Repositories;
using KafkaFlow;
using KafkaFlow.Consumers;
using KafkaFlow.Producers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BankMore.Tarifas.Infrastructure.Messaging;

public class TransferRealizedMessageHandler : IMessageMiddleware
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TransferRealizedMessageHandler> _logger;
    private readonly IProducerAccessor _producerAccessor;

    public TransferRealizedMessageHandler(
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration,
        ILogger<TransferRealizedMessageHandler> logger,
        IProducerAccessor producerAccessor)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
        _logger = logger;
        _producerAccessor = producerAccessor;
    }

    public async Task Invoke(IMessageContext context, MiddlewareDelegate next)
    {
        _logger.LogInformation("=== TransferRealizedMessageHandler Invoke chamado ===");

        using var scope = _serviceScopeFactory.CreateScope();
        var feeRepository = scope.ServiceProvider.GetRequiredService<IFeeRepository>();

        try
        {
            // Extrai a mensagem do contexto
            // No KafkaFlow 2.0, sem serializer, a mensagem vem como byte[]
            string messageJson;
            var messageObj = context.Message;
            
            _logger.LogInformation("Tipo da mensagem: {Type}", messageObj?.GetType().FullName ?? "null");
            
            if (messageObj is byte[] bytes)
            {
                messageJson = System.Text.Encoding.UTF8.GetString(bytes);
                _logger.LogInformation("Mensagem convertida de byte[] para string. Tamanho: {Size}", messageJson.Length);
            }
            else if (messageObj is string str)
            {
                messageJson = str;
                _logger.LogInformation("Mensagem já é string. Tamanho: {Size}", messageJson.Length);
            }
            else
            {
                // Tenta converter para string
                messageJson = messageObj?.ToString() ?? string.Empty;
                _logger.LogWarning("Mensagem convertida via ToString(). Tipo original: {Type}", 
                    messageObj?.GetType().FullName ?? "null");
            }
            
            _logger.LogInformation("Mensagem JSON recebida: {MessageJson}", messageJson);
            
            if (string.IsNullOrEmpty(messageJson))
            {
                _logger.LogWarning("Mensagem de transferência recebida é vazia. Tipo: {Type}", 
                    messageObj?.GetType().Name ?? "null");
                await next(context);
                return;
            }

            var message = JsonSerializer.Deserialize<TransferRealizedMessage>(messageJson);
            if (message == null)
            {
                _logger.LogWarning("Mensagem de transferência recebida é nula ou inválida.");
                return;
            }

            _logger.LogInformation(
                "Transferência recebida: RequestId={RequestId}, AccountNumber={AccountNumber}, Amount={TransferAmount}",
                message.RequestId, message.AccountNumber, message.TransferAmount);

            // Verifica se a tarifa já foi aplicada para evitar duplicidade (idempotência)
            var existingFee = await feeRepository.GetByTransferIdAsync(message.RequestId);
            if (existingFee != null)
            {
                _logger.LogInformation(
                    "Tarifa já foi aplicada anteriormente para transferência {RequestId}",
                    message.RequestId);
                return;
            }

            // Obtém o valor da tarifa do appsettings (2 reais)
            var feeAmount = decimal.Parse(_configuration["Fees:FlatAmount"] ?? "2.00");
            var transferAmount = message.TransferAmount;
            
            // Cria a taxa
            var fee = Fee.Create(message.RequestId, message.AccountNumber, transferAmount, feeAmount);
            await feeRepository.CreateAsync(fee);

            _logger.LogInformation(
                "Tarifa de {FeeAmount} aplicada para transferência {RequestId} na conta {AccountNumber}",
                fee.FeeAmount, fee.TransferId, fee.AccountNumber);

            // Publica mensagem de tarifa aplicada para o Accounts API
            try
            {
                var feeAppliedMessage = new FeeAppliedMessage
                {
                    AccountNumber = fee.AccountNumber,
                    FeeAmount = fee.FeeAmount
                };
                var feeAppliedMessageJson = JsonSerializer.Serialize(feeAppliedMessage);
                
                var producer = _producerAccessor.GetProducer("fees-producer");
                // KafkaFlow 2.0 espera byte[] quando não há serializer configurado
                var messageBytes = System.Text.Encoding.UTF8.GetBytes(feeAppliedMessageJson);
                await producer.ProduceAsync(fee.AccountNumber, messageBytes);
                
                _logger.LogInformation(
                    "Mensagem de tarifa aplicada publicada para conta {AccountNumber}",
                    fee.AccountNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar mensagem de tarifa aplicada");
                throw;
            }

            // Chama o próximo middleware no pipeline (se houver)
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado no consumer de transferências.");
            throw;
        }
    }
}
