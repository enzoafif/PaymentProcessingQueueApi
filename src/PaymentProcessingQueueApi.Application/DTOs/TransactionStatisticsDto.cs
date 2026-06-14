namespace PaymentProcessingQueueApi.Application.DTOs;

/// <summary>Contagem de transações por status, retornada pelo endpoint de estatísticas.</summary>
public sealed record TransactionStatisticsDto(
    int Waiting,
    int Processing,
    int Completed,
    int Deleted,
    int Total);
