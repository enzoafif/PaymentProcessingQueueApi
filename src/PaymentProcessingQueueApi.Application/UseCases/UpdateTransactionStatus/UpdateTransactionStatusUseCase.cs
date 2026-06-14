using Microsoft.Extensions.Logging;
using PaymentProcessingQueueApi.Application.DTOs;
using PaymentProcessingQueueApi.Application.Exceptions;
using PaymentProcessingQueueApi.Application.Interfaces;
using PaymentProcessingQueueApi.Domain.Abstractions;
using PaymentProcessingQueueApi.Domain.Enums;
using PaymentProcessingQueueApi.Domain.PriorityRules;

namespace PaymentProcessingQueueApi.Application.UseCases.UpdateTransactionStatus;

/// <summary>
/// Caso de uso "atualizar status": altera apenas o status da transação.
/// Não permite definir status Deleted (use DELETE para isso).
/// </summary>
public sealed class UpdateTransactionStatusUseCase
{
    private readonly ITransactionRepository _repository;
    private readonly IPriorityRule _priorityRule;
    private readonly IClock _clock;
    private readonly ILogger<UpdateTransactionStatusUseCase> _logger;

    public UpdateTransactionStatusUseCase(
        ITransactionRepository repository,
        IPriorityRule priorityRule,
        IClock clock,
        ILogger<UpdateTransactionStatusUseCase> logger)
    {
        _repository = repository;
        _priorityRule = priorityRule;
        _clock = clock;
        _logger = logger;
    }

    public async Task<TransactionDto> ExecuteAsync(Guid id, TransactionStatus newStatus, CancellationToken cancellationToken = default)
    {
        var transaction = await _repository.GetByIdAsync(id, cancellationToken);
        if (transaction is null || !transaction.Active)
            throw new ResourceNotFoundException($"Transação {id} não encontrada.");

        transaction.UpdateStatus(newStatus, _clock.Now);
        await _repository.UpdateAsync(transaction, cancellationToken);

        _logger.LogInformation("Status da transação {Id} alterado para {Status}.", id, newStatus);

        var priority = _priorityRule.Calculate(transaction, transaction.CreatedAt);
        return TransactionMapper.ToDto(transaction, null, priority);
    }
}
