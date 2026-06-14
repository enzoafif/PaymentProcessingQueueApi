namespace PaymentProcessingQueueApi.Application.DTOs;

/// <summary>Resultado paginado genérico retornado pelos casos de uso de listagem.</summary>
public sealed record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int TotalItems,
    int TotalPages,
    int CurrentPage,
    int PageSize);
