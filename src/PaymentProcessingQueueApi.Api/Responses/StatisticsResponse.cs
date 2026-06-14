namespace PaymentProcessingQueueApi.Api.Responses;

/// <summary>Contagem de transações por status.</summary>
public sealed class StatisticsResponse
{
    public int Waiting { get; init; }
    public int Processing { get; init; }
    public int Completed { get; init; }
    public int Deleted { get; init; }
    public int Total { get; init; }
}
