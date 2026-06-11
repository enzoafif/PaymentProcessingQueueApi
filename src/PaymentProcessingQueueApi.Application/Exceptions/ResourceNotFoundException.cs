namespace PaymentProcessingQueueApi.Application.Exceptions;

/// <summary>
/// Lançada quando um recurso solicitado não existe ou não está disponível para consulta
/// (por exemplo, uma transação inexistente ou excluída logicamente). Mapeada para HTTP 404.
/// </summary>
public sealed class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string message) : base(message) { }
}
