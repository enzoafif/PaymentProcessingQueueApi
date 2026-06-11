namespace PaymentProcessingQueueApi.Domain.Abstractions;

/// <summary>
/// Abstração de tempo. Isola o domínio do acesso direto a <see cref="DateTime"/>,
/// favorecendo testabilidade e o princípio de Inversão de Dependência (SOLID).
/// </summary>
public interface IClock
{
    /// <summary>Momento atual.</summary>
    DateTime Now { get; }
}
