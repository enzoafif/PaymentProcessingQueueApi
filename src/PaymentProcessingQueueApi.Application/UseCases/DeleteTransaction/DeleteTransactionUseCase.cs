using Microsoft.Extensions.Logging;
using PaymentProcessingQueueApi.Application.Exceptions;
using PaymentProcessingQueueApi.Application.Interfaces;
using PaymentProcessingQueueApi.Domain.Abstractions;

namespace PaymentProcessingQueueApi.Application.UseCases.DeleteTransaction;

/// <summary>
/// Caso de uso "excluir transação". Implementa EXCLUSÃO LÓGICA: o registro permanece no
/// banco, apenas com o status alterado para excluída. Tentar excluir duas vezes é
/// tratado como conflito pela própria regra de domínio (<c>SoftDelete</c>).
/// </summary>
public sealed class DeleteTransactionUseCase
{
    private readonly ITransactionRepository _repository;
    private readonly IClock _clock;
    private readonly ILogger<DeleteTransactionUseCase> _logger;

    public DeleteTransactionUseCase(
        ITransactionRepository repository,
        IClock clock,
        ILogger<DeleteTransactionUseCase> logger)
    {
        _repository = repository;
        _clock = clock;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await _repository.GetByIdAsync(id, cancellationToken);
        if (transaction is null)
            throw new ResourceNotFoundException($"Transação {id} não encontrada.");

        // Lança BusinessRuleException (=> 409) se já estiver excluída.
        transaction.SoftDelete(_clock.Now);
        await _repository.UpdateAsync(transaction, cancellationToken);

        _logger.LogInformation("Transação {Id} excluída logicamente em {DeletedAt}.", id, transaction.DeletedAt);
    }
}
