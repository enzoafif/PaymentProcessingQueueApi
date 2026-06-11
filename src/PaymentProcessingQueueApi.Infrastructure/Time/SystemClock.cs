using PaymentProcessingQueueApi.Domain.Abstractions;

namespace PaymentProcessingQueueApi.Infrastructure.Time;

/// <summary>
/// Implementação de <see cref="IClock"/> baseada no relógio do sistema.
///
/// Usa <c>DateTime.Now</c> (hora local) PROPOSITALMENTE: os horários limite (cutoff) são
/// informados em horário de parede local (sem offset), então comparar com a hora local mantém
/// o cálculo de prioridade consistente. Em um ambiente distribuído/produção, o ideal seria
/// padronizar tudo em <c>DateTimeOffset</c>/UTC (inclusive o contrato da API) — graças à
/// abstração <see cref="IClock"/>, essa troca fica isolada apenas nesta classe.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}
