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

    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        // A entidade normalmente vem rastreada de GetByIdAsync, então o SaveChanges já persiste
        // a mutação (ex.: SoftDelete) gravando apenas as colunas alteradas. Só chamamos Update()
        // se a entidade estiver desanexada — evitando marcar todas as colunas como modificadas.
        if (_context.Entry(transaction).State == EntityState.Detached)
            _context.Transactions.Update(transaction);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
