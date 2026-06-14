using PaymentProcessingQueueApi.Domain.Entities;
using PaymentProcessingQueueApi.Domain.Enums;

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

    /// <summary>
    /// Lista transações ativas paginadas, ordenadas por prioridade decrescente e desempate por CreatedAt.
    /// Retorna os itens da página e o total de itens ativos.
    /// </summary>
    Task<(IReadOnlyList<Transaction> Items, int TotalItems)> GetPagedAsync(int page, int size, CancellationToken cancellationToken = default);

    /// <summary>Busca transações ativas cuja descrição contém o termo informado (case-insensitive).</summary>
    Task<IReadOnlyList<Transaction>> SearchByDescriptionAsync(string descricao, CancellationToken cancellationToken = default);

    /// <summary>Retorna a contagem de transações agrupada por status.</summary>
    Task<Dictionary<TransactionStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>Persiste alterações de uma transação já existente (ex.: exclusão lógica).</summary>
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
