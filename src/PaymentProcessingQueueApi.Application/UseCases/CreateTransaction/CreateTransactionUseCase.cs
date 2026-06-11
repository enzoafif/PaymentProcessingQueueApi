using Microsoft.Extensions.Logging;
using PaymentProcessingQueueApi.Application.DTOs;
using PaymentProcessingQueueApi.Application.Interfaces;
using PaymentProcessingQueueApi.Domain.Abstractions;
using PaymentProcessingQueueApi.Domain.Entities;
using PaymentProcessingQueueApi.Domain.PriorityRules;
using PaymentProcessingQueueApi.Domain.Services;

namespace PaymentProcessingQueueApi.Application.UseCases.CreateTransaction;

/// <summary>
/// Caso de uso "cadastrar transação": cria a entidade, calcula a prioridade pela regra de
/// negócio, persiste e devolve a posição atual na fila de prioridade (apoiada em Heap).
/// </summary>
public sealed class CreateTransactionUseCase
{
    private readonly ITransactionRepository _repository;
    private readonly IPriorityRule _priorityRule;
    private readonly TransactionPriorityQueue _priorityQueue;
    private readonly IClock _clock;
    private readonly ILogger<CreateTransactionUseCase> _logger;

    public CreateTransactionUseCase(
        ITransactionRepository repository,
        IPriorityRule priorityRule,
        TransactionPriorityQueue priorityQueue,
        IClock clock,
        ILogger<CreateTransactionUseCase> logger)
    {
        _repository = repository;
        _priorityRule = priorityRule;
        _priorityQueue = priorityQueue;
        _clock = clock;
        _logger = logger;
    }

    public async Task<TransactionDto> ExecuteAsync(CreateTransactionCommand command, CancellationToken cancellationToken = default)
    {
        var now = _clock.Now;

        // 1) cria a entidade (valida invariantes de domínio)
        var transaction = Transaction.Create(
            command.Cpf, command.Description, command.Reference, command.Amount,
            command.Type, command.ClientType, command.FraudRisk, command.CutoffTime, now);

        // 2) calcula e atribui a prioridade derivada dos dados (não informada pelo usuário)
        var priority = _priorityRule.Calculate(transaction, now);
        transaction.AssignPriority(priority.Total);

        // 3) persiste
        await _repository.AddAsync(transaction, cancellationToken);

        // 4) calcula a posição na fila usando o Heap sobre as transações ativas
        var active = await _repository.GetActiveAsync(cancellationToken);
        var position = _priorityQueue.PositionInQueue(transaction.Id, active);

        _logger.LogInformation(
            "Transação {Id} cadastrada com prioridade {Priority} (posição {Position} na fila).",
            transaction.Id, transaction.Priority, position);

        return TransactionMapper.ToDto(transaction, position, priority);
    }
}
