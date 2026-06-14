using PaymentProcessingQueueApi.Application.DTOs;
using PaymentProcessingQueueApi.Application.Interfaces;
using PaymentProcessingQueueApi.Domain.Enums;
using PaymentProcessingQueueApi.Domain.PriorityRules;
using PaymentProcessingQueueApi.Domain.Services;

namespace PaymentProcessingQueueApi.Application.UseCases.GetNextTransaction;

/// <summary>
/// Caso de uso "consultar próxima transação": retorna a transação de maior prioridade
/// com status Waiting sem alterar nenhum dado (somente leitura).
/// </summary>
public sealed class GetNextTransactionUseCase
{
    private readonly ITransactionRepository _repository;
    private readonly IPriorityRule _priorityRule;
    private readonly TransactionPriorityQueue _priorityQueue;

    public GetNextTransactionUseCase(
        ITransactionRepository repository,
        IPriorityRule priorityRule,
        TransactionPriorityQueue priorityQueue)
    {
        _repository = repository;
        _priorityRule = priorityRule;
        _priorityQueue = priorityQueue;
    }

    public async Task<TransactionDto?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var active = await _repository.GetActiveAsync(cancellationToken);
        var waiting = active.Where(t => t.Status == TransactionStatus.Waiting).ToList();

        var next = _priorityQueue.Next(waiting);
        if (next is null) return null;

        var position = _priorityQueue.PositionInQueue(next.Id, waiting);
        var priority = _priorityRule.Calculate(next, next.CreatedAt);

        return TransactionMapper.ToDto(next, position, priority);
    }
}
