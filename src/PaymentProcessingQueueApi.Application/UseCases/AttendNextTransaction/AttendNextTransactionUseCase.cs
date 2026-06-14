using Microsoft.Extensions.Logging;
using PaymentProcessingQueueApi.Application.DTOs;
using PaymentProcessingQueueApi.Application.Exceptions;
using PaymentProcessingQueueApi.Application.Interfaces;
using PaymentProcessingQueueApi.Domain.Abstractions;
using PaymentProcessingQueueApi.Domain.Enums;
using PaymentProcessingQueueApi.Domain.PriorityRules;
using PaymentProcessingQueueApi.Domain.Services;

namespace PaymentProcessingQueueApi.Application.UseCases.AttendNextTransaction;

/// <summary>
/// Caso de uso "atender próxima transação": seleciona a de maior prioridade (Waiting)
/// e muda seu status para Processing.
/// </summary>
public sealed class AttendNextTransactionUseCase
{
    private readonly ITransactionRepository _repository;
    private readonly IPriorityRule _priorityRule;
    private readonly TransactionPriorityQueue _priorityQueue;
    private readonly IClock _clock;
    private readonly ILogger<AttendNextTransactionUseCase> _logger;

    public AttendNextTransactionUseCase(
        ITransactionRepository repository,
        IPriorityRule priorityRule,
        TransactionPriorityQueue priorityQueue,
        IClock clock,
        ILogger<AttendNextTransactionUseCase> logger)
    {
        _repository = repository;
        _priorityRule = priorityRule;
        _priorityQueue = priorityQueue;
        _clock = clock;
        _logger = logger;
    }

    public async Task<TransactionDto> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var active = await _repository.GetActiveAsync(cancellationToken);
        var waiting = active.Where(t => t.Status == TransactionStatus.Waiting).ToList();

        var next = _priorityQueue.Next(waiting);
        if (next is null)
            throw new ResourceNotFoundException("Não há transações aguardando processamento na fila.");

        next.UpdateStatus(TransactionStatus.Processing, _clock.Now);
        await _repository.UpdateAsync(next, cancellationToken);

        _logger.LogInformation("Transação {Id} (prioridade {Priority}) iniciada para processamento.", next.Id, next.Priority);

        var priority = _priorityRule.Calculate(next, next.CreatedAt);
        return TransactionMapper.ToDto(next, null, priority);
    }
}
