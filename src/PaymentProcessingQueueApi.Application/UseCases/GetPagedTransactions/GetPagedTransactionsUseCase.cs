using PaymentProcessingQueueApi.Application.DTOs;
using PaymentProcessingQueueApi.Application.Interfaces;
using PaymentProcessingQueueApi.Domain.Enums;
using PaymentProcessingQueueApi.Domain.PriorityRules;
using PaymentProcessingQueueApi.Domain.Services;

namespace PaymentProcessingQueueApi.Application.UseCases.GetPagedTransactions;

/// <summary>
/// Caso de uso "listar transações paginadas": retorna transações ativas ordenadas por
/// prioridade decrescente, com metadados de paginação e a posição de cada transação
/// Waiting na árvore do heap binário.
/// </summary>
public sealed class GetPagedTransactionsUseCase
{
    private readonly ITransactionRepository _repository;
    private readonly IPriorityRule _priorityRule;
    private readonly TransactionPriorityQueue _priorityQueue;

    public GetPagedTransactionsUseCase(
        ITransactionRepository repository,
        IPriorityRule priorityRule,
        TransactionPriorityQueue priorityQueue)
    {
        _repository = repository;
        _priorityRule = priorityRule;
        _priorityQueue = priorityQueue;
    }

    public async Task<PagedResultDto<TransactionDto>> ExecuteAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 10;

        // Carrega todas as transações ativas para construir o heap completo.
        var all = await _repository.GetActiveAsync(cancellationToken);

        // Constrói o heap apenas com as transações Waiting (as que estão na fila real).
        var waiting = all.Where(t => t.Status == TransactionStatus.Waiting).ToList();
        var heapInfo = _priorityQueue.GetAllWithHeapInfo(waiting)
            .ToDictionary(x => x.Transaction.Id, x => x.HeapIndex);

        // Ordena todas as ativas por prioridade desc + CreatedAt asc (ordem de atendimento).
        var ordered = all
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToList();

        var totalItems = ordered.Count;
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling((double)totalItems / size);

        var page_items = ordered
            .Skip((page - 1) * size)
            .Take(size)
            .Select(t =>
            {
                heapInfo.TryGetValue(t.Id, out var heapIndex);
                var hasHeapInfo = t.Status == TransactionStatus.Waiting;
                return TransactionMapper.ToDto(
                    t,
                    positionInQueue: null,
                    _priorityRule.Calculate(t, t.CreatedAt),
                    heapIndex: hasHeapInfo ? heapIndex : null);
            })
            .ToList();

        return new PagedResultDto<TransactionDto>(page_items, totalItems, totalPages, page, size);
    }
}
