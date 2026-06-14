using Microsoft.EntityFrameworkCore;
using PaymentProcessingQueueApi.Domain.Abstractions;
using PaymentProcessingQueueApi.Domain.Entities;
using PaymentProcessingQueueApi.Domain.Enums;
using PaymentProcessingQueueApi.Domain.PriorityRules;

namespace PaymentProcessingQueueApi.Infrastructure.Persistence;

/// <summary>
/// Massa inicial de dados (seed) para facilitar a demonstração. Cria transações com
/// prioridades bem diferentes — útil para mostrar a ordenação da fila. Os CPFs são válidos
/// (dígitos verificadores corretos) e os horários de criação são levemente escalonados, de
/// modo que o critério de desempate (mais antigo primeiro) seja determinístico.
/// </summary>
public static class TransactionSeeder
{
    // Especificação de cada transação de exemplo (sem o horário de criação, definido no loop).
    private sealed record Sample(
        string Cpf, string Description, string Reference, decimal Amount,
        TransactionType Type, ClientType ClientType, FraudRiskLevel FraudRisk, DateTime CutoffTime);

    public static async Task SeedAsync(AppDbContext context, IPriorityRule priorityRule, IClock clock)
    {
        if (await context.Transactions.AnyAsync())
            return; // evita duplicar a massa

        var baseTime = clock.Now;

        var samples = new[]
        {
            // Alto valor + cutoff muito próximo => prioridade altíssima
            new Sample("11144477735", "PIX para fornecedor urgente", "PIX-001",
                2_500_000m, TransactionType.Pix, ClientType.Corporate,
                FraudRiskLevel.Low, baseTime.AddMinutes(10)),

            // Baixo valor + cutoff distante => prioridade baixa
            new Sample("52998224725", "Pagamento de boleto de tributo", "BOL-002",
                850m, TransactionType.Boleto, ClientType.Standard,
                FraudRiskLevel.Low, baseTime.AddHours(8)),

            // Alto valor + cliente premium + cutoff em 2h => prioridade média/alta
            new Sample("47207183887", "Crédito agendado folha de pagamento", "CRD-003",
                120_000m, TransactionType.Credito, ClientType.Premium,
                FraudRiskLevel.Medium, baseTime.AddHours(2)),

            // Alto valor, porém risco ALTO => penalidade derruba a prioridade
            new Sample("96387304989", "Débito suspeito de alto valor", "DEB-004",
                3_000_000m, TransactionType.Debito, ClientType.Corporate,
                FraudRiskLevel.High, baseTime.AddMinutes(30)),
        };

        for (var i = 0; i < samples.Length; i++)
        {
            var s = samples[i];

            // CreatedAt escalonado torna o desempate determinístico.
            var createdAt = baseTime.AddSeconds(i);

            var transaction = Transaction.Create(
                s.Cpf, s.Description, s.Reference, s.Amount,
                s.Type, s.ClientType, s.FraudRisk, s.CutoffTime, createdAt);

            // Calcula a prioridade usando a MESMA referência gravada em CreatedAt, garantindo
            // que o detalhamento recalculado no GET coincida com o valor armazenado.
            transaction.AssignPriority(priorityRule.Calculate(transaction, createdAt).Total);

            await context.Transactions.AddAsync(transaction);
        }

        await context.SaveChangesAsync();
    }
}
