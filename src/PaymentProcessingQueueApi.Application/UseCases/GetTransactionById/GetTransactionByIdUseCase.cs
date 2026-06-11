using PaymentProcessingQueueApi.Application.DTOs;
using PaymentProcessingQueueApi.Application.Exceptions;
using PaymentProcessingQueueApi.Application.Interfaces;
using PaymentProcessingQueueApi.Domain.PriorityRules;
using PaymentProcessingQueueApi.Domain.Services;

namespace PaymentProcessingQueueApi.Application.UseCases.GetTransactionById;

/// <summary>
/// Caso de uso "consultar transação por id". Aplica a regra de exclusão lógica:
/// transações excluídas NÃO aparecem nas consultas comuns (retornam 404).
/// </summary>
public sealed class GetTransactionByIdUseCase
{
    private readonly ITransactionRepository _repository;
    private readonly IPriorityRule _priorityRule;
    private readonly TransactionPriorityQueue _priorityQueue;

    public GetTransactionByIdUseCase(
        ITransactionRepository repository,
        IPriorityRule priorityRule,
        TransactionPriorityQueue priorityQueue)
    {
        _repository = repository;
        _priorityRule = priorityRule;
        _priorityQueue = priorityQueue;
    }

    public async Task<TransactionDto> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await _repository.GetByIdAsync(id, cancellationToken);

        // Exclusão lógica: itens excluídos não devem aparecer nas consultas comuns.
        if (transaction is null || !transaction.Active)
            throw new ResourceNotFoundException($"Transação {id} não encontrada.");

        var active = await _repository.GetActiveAsync(cancellationToken);
        var position = _priorityQueue.PositionInQueue(transaction.Id, active);

        // Reproduz o detalhamento usando a data de criação como referência, de modo que o
        // total recalculado coincida com a prioridade fixada no cadastro.
        var priority = _priorityRule.Calculate(transaction, transaction.CreatedAt);

        return TransactionMapper.ToDto(transaction, position, priority);
    }
}
