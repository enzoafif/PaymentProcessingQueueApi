using PaymentProcessingQueueApi.Application.DTOs;
using PaymentProcessingQueueApi.Application.Interfaces;
using PaymentProcessingQueueApi.Domain.PriorityRules;

namespace PaymentProcessingQueueApi.Application.UseCases.SearchTransactions;

/// <summary>
/// Caso de uso "buscar transações por descrição": retorna transações ativas cujo campo
/// Description contém o termo informado (case-insensitive), ordenadas por prioridade.
/// </summary>
public sealed class SearchTransactionsUseCase
{
    private readonly ITransactionRepository _repository;
    private readonly IPriorityRule _priorityRule;

    public SearchTransactionsUseCase(ITransactionRepository repository, IPriorityRule priorityRule)
    {
        _repository = repository;
        _priorityRule = priorityRule;
    }

    public async Task<IReadOnlyList<TransactionDto>> ExecuteAsync(string descricao, CancellationToken cancellationToken = default)
    {
        var transactions = await _repository.SearchByDescriptionAsync(descricao, cancellationToken);

        return transactions
            .Select(t => TransactionMapper.ToDto(t, null, _priorityRule.Calculate(t, t.CreatedAt)))
            .ToList();
    }
}
