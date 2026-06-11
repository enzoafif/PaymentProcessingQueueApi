namespace PaymentProcessingQueueApi.Domain.Exceptions;

/// <summary>
/// Lançada quando uma invariante ou regra de negócio do domínio é violada
/// (por exemplo, valor inválido ou exclusão de uma transação já excluída).
/// </summary>
public sealed class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
