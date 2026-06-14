using Microsoft.Extensions.Logging;
using PaymentProcessingQueueApi.Application.DTOs;
using PaymentProcessingQueueApi.Application.Exceptions;
using PaymentProcessingQueueApi.Application.Interfaces;
using PaymentProcessingQueueApi.Domain.Abstractions;
using PaymentProcessingQueueApi.Domain.PriorityRules;
using PaymentProcessingQueueApi.Domain.Services;

namespace PaymentProcessingQueueApi.Application.UseCases.UpdateTransaction;

/// <summary>
/// Caso de uso "atualizar transação": atualiza os campos editáveis, recalcula a prioridade
/// automaticamente e retorna a transação atualizada com a nova posição na fila.
/// </summary>
public sealed class UpdateTransactionUseCase
{
    private readonly ITransactionRepository _repository;
    private readonly IPriorityRule _priorityRule;
    private readonly TransactionPriorityQueue _priorityQueue;
    private readonly IClock _clock;
    private readonly ILogger<UpdateTransactionUseCase> _logger;

    public UpdateTransactionUseCase(
        ITransactionRepository repository,
        IPriorityRule priorityRule,
        TransactionPriorityQueue priorityQueue,
        IClock clock,
        ILogger<UpdateTransactionUseCase> logger)
    {
        _repository = repository;
        _priorityRule = priorityRule;
        _priorityQueue = priorityQueue;
        _clock = clock;
        _logger = logger;
    }

    public async Task<TransactionDto> ExecuteAsync(UpdateTransactionCommand command, CancellationToken cancellationToken = default)
    {
        var transaction = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (transaction is null || !transaction.Active)
            throw new ResourceNotFoundException($"Transação {command.Id} não encontrada.");

        var now = _clock.Now;

        transaction.Update(
            command.Description, command.Reference, command.Amount,
            command.Type, command.ClientType, command.FraudRisk, command.CutoffTime, now);

        // Recalcula a prioridade com o instante atual como referência.
        var priority = _priorityRule.Calculate(transaction, now);
        transaction.AssignPriority(priority.Total);

        await _repository.UpdateAsync(transaction, cancellationToken);

        var active = await _repository.GetActiveAsync(cancellationToken);
        var position = _priorityQueue.PositionInQueue(transaction.Id, active);

        _logger.LogInformation(
            "Transação {Id} atualizada. Nova prioridade: {Priority} (posição {Position}).",
            transaction.Id, transaction.Priority, position);

        return TransactionMapper.ToDto(transaction, position, priority);
    }
}
