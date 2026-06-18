using PaymentProcessingQueueApi.Application.Interfaces;
using PaymentProcessingQueueApi.Domain.DataStructures;
using PaymentProcessingQueueApi.Domain.Entities;
using PaymentProcessingQueueApi.Domain.Enums;
using PaymentProcessingQueueApi.Domain.Services;

namespace PaymentProcessingQueueApi.Application.UseCases.GetTransacoesResumidas;

public sealed record TransacaoResumidaDto(
    string Descricao,
    int Prioridade,
    int IndiceNoHeap,
    string PapelNoHeap);

/// <summary>
/// Retorna apenas as transações Waiting com descrição, prioridade e posição no heap —
/// ordenadas por índice (nível a nível na árvore) para facilitar a visualização em aula.
/// </summary>
public sealed class GetTransacoesResumidasUseCase
{
    private readonly ITransactionRepository _repository;
    private readonly TransactionPriorityQueue _priorityQueue;

    public GetTransacoesResumidasUseCase(
        ITransactionRepository repository,
        TransactionPriorityQueue priorityQueue)
    {
        _repository = repository;
        _priorityQueue = priorityQueue;
    }

    public async Task<IReadOnlyList<TransacaoResumidaDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var all = await _repository.GetActiveAsync(cancellationToken);
        var waiting = all.Where(t => t.Status == TransactionStatus.Waiting).ToList();

        return _priorityQueue.GetAllWithHeapInfo(waiting)
            .OrderBy(x => x.HeapIndex)
            .Select(x => new TransacaoResumidaDto(
                x.Transaction.Description,
                x.Transaction.Priority,
                x.HeapIndex,
                BinaryHeap<Transaction>.RoleLabel(x.HeapIndex)))
            .ToList();
    }
}
