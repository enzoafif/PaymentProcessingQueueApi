namespace PaymentProcessingQueueApi.Api.Responses;

/// <summary>Resultado paginado de transações.</summary>
public sealed class PagedTransactionResponse
{
    public IReadOnlyList<TransactionResponse> Items { get; init; } = [];
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
}
