using PaymentProcessingQueueApi.Application.DTOs;
using PaymentProcessingQueueApi.Application.Interfaces;
using PaymentProcessingQueueApi.Domain.PriorityRules;

namespace PaymentProcessingQueueApi.Application.UseCases.GetPagedTransactions;

/// <summary>
/// Caso de uso "listar transações paginadas": retorna transações ativas ordenadas por
/// prioridade decrescente, com metadados de paginação.
/// </summary>
public sealed class GetPagedTransactionsUseCase
{
    private readonly ITransactionRepository _repository;
    private readonly IPriorityRule _priorityRule;

    public GetPagedTransactionsUseCase(ITransactionRepository repository, IPriorityRule priorityRule)
    {
        _repository = repository;
        _priorityRule = priorityRule;
    }

    public async Task<PagedResultDto<TransactionDto>> ExecuteAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 10;

        var (items, totalItems) = await _repository.GetPagedAsync(page, size, cancellationToken);

        var dtos = items
            .Select(t => TransactionMapper.ToDto(t, null, _priorityRule.Calculate(t, t.CreatedAt)))
            .ToList();

        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling((double)totalItems / size);

        return new PagedResultDto<TransactionDto>(dtos, totalItems, totalPages, page, size);
    }
}
