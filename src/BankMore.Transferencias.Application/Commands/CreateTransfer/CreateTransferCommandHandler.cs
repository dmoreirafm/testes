using BankMore.Transferencias.Application.Services;
using BankMore.Transferencias.Domain.Entities;
using BankMore.Transferencias.Domain.Enums;
using BankMore.Transferencias.Domain.Messages;
using BankMore.Transferencias.Domain.Repositories;
using KafkaFlow;
using KafkaFlow.Producers;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BankMore.Transferencias.Application.Commands.CreateTransfer;

public class CreateTransferCommandHandler : IRequestHandler<CreateTransferCommand, CreateTransferResponse>
{
    private readonly ITransferRepository _transferRepository;
    private readonly IAccountsApiClient _accountsApiClient;
    private readonly ILogger<CreateTransferCommandHandler> _logger;
    private readonly IProducerAccessor _producerAccessor;
    private readonly IConfiguration _configuration;

    public CreateTransferCommandHandler(
        ITransferRepository transferRepository,
        IAccountsApiClient accountsApiClient,
        ILogger<CreateTransferCommandHandler> logger,
        IProducerAccessor producerAccessor,
        IConfiguration configuration)
    {
        _transferRepository = transferRepository;
        _accountsApiClient = accountsApiClient;
        _logger = logger;
        _producerAccessor = producerAccessor;
        _configuration = configuration;
    }

    public async Task<CreateTransferResponse> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
    {
        // Valida campos obrigatórios
        if (string.IsNullOrWhiteSpace(request.OriginAccountNumber))
            throw new Domain.Common.DomainException("Conta de origem não informada.", "INVALID_ACCOUNT");

        if (string.IsNullOrWhiteSpace(request.DestinationAccountNumber))
            throw new Domain.Common.DomainException("Conta de destino não informada.", "INVALID_ACCOUNT");

        if (string.IsNullOrWhiteSpace(request.JwtToken))
            throw new Domain.Common.DomainException("Token JWT não fornecido.", "INVALID_TOKEN");

        // Valida valor
        if (request.Amount <= 0)
            throw new Domain.Common.DomainException("Apenas valores positivos podem ser recebidos.", "INVALID_VALUE");

        var originAccountNumber = request.OriginAccountNumber;
        var jwtToken = request.JwtToken;

        // Valida idempotência
        var existingTransfer = await _transferRepository.GetByRequestIdAsync(request.RequestId, cancellationToken);
        if (existingTransfer != null)
        {
            _logger.LogInformation("Transferência com RequestId {RequestId} já processada.", request.RequestId);
            return new CreateTransferResponse
            {
                TransferId = existingTransfer.Id.ToString(),
                OriginAccountNumber = existingTransfer.OriginAccountNumber,
                DestinationAccountNumber = existingTransfer.DestinationAccountNumber,
                Amount = existingTransfer.Amount,
                Status = existingTransfer.Status.ToString(),
                CreatedAt = existingTransfer.CreatedAt
            };
        }

        // Valida se conta de destino existe e está ativa (via GetBalance que já valida isso)
        try
        {
            await _accountsApiClient.GetBalanceAsync(request.DestinationAccountNumber, jwtToken, cancellationToken);
            _logger.LogInformation("Conta de destino {DestinationAccount} validada (existe e está ativa)", 
                request.DestinationAccountNumber);
        }
        catch (Domain.Common.DomainException ex)
        {
            // Propaga os erros de validação de conta (INVALID_ACCOUNT, INACTIVE_ACCOUNT)
            if (ex.ErrorCode == "INVALID_ACCOUNT" || ex.ErrorCode == "INACTIVE_ACCOUNT")
            {
                throw;
            }
            // Se for outro erro, loga mas continua (a validação será feita na hora do crédito)
            _logger.LogWarning(ex, "Aviso ao validar conta de destino {DestinationAccount}", request.DestinationAccountNumber);
        }
        catch (Exception ex)
        {
            // Se não conseguir validar, continua (a validação será feita na hora do crédito)
            _logger.LogWarning(ex, "Não foi possível validar conta de destino {DestinationAccount} antecipadamente", 
                request.DestinationAccountNumber);
        }
        
        // Cria transferência com status Pending
        var transfer = Transfer.Create(request.RequestId, originAccountNumber, request.DestinationAccountNumber, request.Amount);
        transfer = await _transferRepository.CreateAsync(transfer, cancellationToken);

        try
        {
            // Passo 1: Débito na conta de origem
            _logger.LogInformation("Iniciando débito na conta de origem {OriginAccount} no valor de {Amount}", 
                originAccountNumber, request.Amount);

            var debitRequestId = $"{request.RequestId}-debit";
            await _accountsApiClient.MakeDebitAsync(
                debitRequestId,
                originAccountNumber,
                request.Amount,
                jwtToken,
                cancellationToken);

            _logger.LogInformation("Débito realizado com sucesso na conta {OriginAccount}", originAccountNumber);

            // Passo 2: Crédito na conta de destino
            _logger.LogInformation("Iniciando crédito na conta de destino {DestinationAccount} no valor de {Amount}", 
                request.DestinationAccountNumber, request.Amount);

            var creditRequestId = $"{request.RequestId}-credit";
            await _accountsApiClient.MakeCreditAsync(
                creditRequestId,
                request.DestinationAccountNumber,
                request.Amount,
                jwtToken,
                cancellationToken);

            _logger.LogInformation("Crédito realizado com sucesso na conta {DestinationAccount}", request.DestinationAccountNumber);

            // Marca transferência como concluída
            transfer.MarkAsCompleted();
            await _transferRepository.UpdateAsync(transfer, cancellationToken);

            // Publica mensagem para o tópico de transferências realizadas
            try
            {
                var topicName = _configuration["Kafka:Topics:TransferRealized"] ?? "transferencias-realizadas";
                var message = new TransferRealizedMessage
                {
                    RequestId = request.RequestId,
                    AccountNumber = originAccountNumber,
                    TransferAmount = request.Amount
                };

                var messageJson = JsonSerializer.Serialize(message);
                var producer = _producerAccessor.GetProducer("transfers-producer");
                
                // KafkaFlow 2.0 espera byte[] quando não há serializer configurado
                var messageBytes = System.Text.Encoding.UTF8.GetBytes(messageJson);
                await producer.ProduceAsync(message.AccountNumber, messageBytes);
                
                _logger.LogInformation(
                    "Mensagem de transferência publicada para conta {AccountNumber}",
                    message.AccountNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao publicar mensagem de transferência, mas a transferência foi concluída com sucesso");
                // Não falhamos a transferência se a publicação da mensagem falhar
            }

            return new CreateTransferResponse
            {
                TransferId = transfer.Id.ToString(),
                OriginAccountNumber = transfer.OriginAccountNumber,
                DestinationAccountNumber = transfer.DestinationAccountNumber,
                Amount = transfer.Amount,
                Status = transfer.Status.ToString(),
                CreatedAt = transfer.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar transferência {RequestId}. Tentando compensação.", request.RequestId);

            // Se o crédito falhou, precisa fazer estorno (compensação) no débito
            if (transfer.Status == TransferStatus.Pending)
            {
                try
                {
                    _logger.LogInformation("Realizando estorno (compensação) na conta {OriginAccount}", originAccountNumber);
                    
                    var compensationRequestId = $"{request.RequestId}-compensation";
                    await _accountsApiClient.MakeCreditAsync(
                        compensationRequestId,
                        originAccountNumber,
                        request.Amount,
                        jwtToken,
                        cancellationToken);

                    transfer.MarkAsCompensated();
                    _logger.LogInformation("Estorno realizado com sucesso na conta {OriginAccount}", originAccountNumber);
                }
                catch (Exception compensationEx)
                {
                    _logger.LogError(compensationEx, "Erro crítico ao realizar estorno na conta {OriginAccount}. Ação manual pode ser necessária.", 
                        originAccountNumber);
                    transfer.MarkAsFailed($"Erro na compensação: {compensationEx.Message}");
                }
            }
            else
            {
                transfer.MarkAsFailed($"Erro: {ex.Message}");
            }

            await _transferRepository.UpdateAsync(transfer, cancellationToken);

            throw new Domain.Common.DomainException(
                $"Transferência falhou: {ex.Message}. Status: {transfer.Status}",
                "TRANSFER_FAILED");
        }
    }
}

