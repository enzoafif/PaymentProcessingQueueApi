using PaymentProcessingQueueApi.Domain.Entities;

namespace PaymentProcessingQueueApi.Application.Interfaces;

/// <summary>
/// Abstração de persistência de transações. Definida na camada de Aplicação (que a consome)
/// e implementada na Infraestrutura — Inversão de Dependência (a Aplicação não conhece o EF Core).
/// </summary>
public interface ITransactionRepository
{
    /// <summary>Persiste uma nova transação.</summary>
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>Obtém uma transação por id, independentemente do status (inclui excluídas).</summary>
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lista as transações ativas (não excluídas) — base para a fila de prioridade.</summary>
    Task<IReadOnlyList<Transaction>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Persiste alterações de uma transação já existente (ex.: exclusão lógica).</summary>
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
