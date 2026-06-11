using Microsoft.EntityFrameworkCore;
using PaymentProcessingQueueApi.Domain.Entities;

namespace PaymentProcessingQueueApi.Infrastructure.Persistence;

/// <summary>
/// Contexto de persistência (EF Core). Atualmente usa o provedor InMemory, mas a troca
/// por um banco relacional (SQL Server, PostgreSQL) exige apenas mudar o registro em
/// <c>AddInfrastructure</c> — o restante do código não muda.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas as configurações IEntityTypeConfiguration deste assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
