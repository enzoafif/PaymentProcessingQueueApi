using Microsoft.EntityFrameworkCore;
using PaymentProcessingQueueApi.Application.Interfaces;
using PaymentProcessingQueueApi.Domain.Entities;
using PaymentProcessingQueueApi.Domain.Enums;
using PaymentProcessingQueueApi.Infrastructure.Persistence;

namespace PaymentProcessingQueueApi.Infrastructure.Repositories;

/// <summary>Implementação de <see cref="ITransactionRepository"/> sobre o EF Core.</summary>
public sealed class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _context;

    public TransactionRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await _context.Transactions
            .Where(t => t.Active && t.Status != TransactionStatus.Deleted)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Transaction> Items, int TotalItems)> GetPagedAsync(
        int page, int size, CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions
            .Where(t => t.Active && t.Status != TransactionStatus.Deleted)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public async Task<IReadOnlyList<Transaction>> SearchByDescriptionAsync(
        string descricao, CancellationToken cancellationToken = default)
        => await _context.Transactions
            .Where(t => t.Active
                        && t.Status != TransactionStatus.Deleted
                        && EF.Functions.ILike(t.Description, $"%{descricao}%"))
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<Dictionary<TransactionStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _context.Transactions
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return raw.ToDictionary(x => x.Status, x => x.Count);
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        // A entidade normalmente vem rastreada de GetByIdAsync, então o SaveChanges já persiste
        // a mutação gravando apenas as colunas alteradas. Só chamamos Update() se estiver desanexada.
        if (_context.Entry(transaction).State == EntityState.Detached)
            _context.Transactions.Update(transaction);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
