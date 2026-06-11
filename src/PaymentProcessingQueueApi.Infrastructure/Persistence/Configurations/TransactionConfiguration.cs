using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentProcessingQueueApi.Domain.Entities;

namespace PaymentProcessingQueueApi.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento da entidade <see cref="Transaction"/>. Mantido na Infraestrutura para que o
/// domínio não dependa do EF Core. Enums são gravados como texto (legível e estável p/ SQL).
/// </summary>
public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        // Obs.: ToTable() pertence ao provedor relacional; com o InMemory o nome padrão
        // já é "Transactions" (nome do DbSet). Ao migrar para SQL, basta reativá-lo.
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Cpf).IsRequired().HasMaxLength(14);
        builder.Property(t => t.Description).IsRequired().HasMaxLength(500);
        builder.Property(t => t.Reference).HasMaxLength(100);
        builder.Property(t => t.Amount).HasPrecision(18, 2);

        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(t => t.ClientType).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.FraudRisk).HasConversion<string>().HasMaxLength(10);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

        // Índices úteis quando migrar para um banco relacional.
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Cpf);
    }
}
