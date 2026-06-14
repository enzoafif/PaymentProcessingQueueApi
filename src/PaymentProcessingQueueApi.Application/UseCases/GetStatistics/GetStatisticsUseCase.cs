using PaymentProcessingQueueApi.Application.DTOs;
using PaymentProcessingQueueApi.Application.Interfaces;
using PaymentProcessingQueueApi.Domain.Enums;

namespace PaymentProcessingQueueApi.Application.UseCases.GetStatistics;

/// <summary>
/// Caso de uso "estatísticas": retorna a contagem de transações agrupada por status.
/// </summary>
public sealed class GetStatisticsUseCase
{
    private readonly ITransactionRepository _repository;

    public GetStatisticsUseCase(ITransactionRepository repository) => _repository = repository;

    public async Task<TransactionStatisticsDto> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var counts = await _repository.GetStatusCountsAsync(cancellationToken);

        counts.TryGetValue(TransactionStatus.Waiting,    out var waiting);
        counts.TryGetValue(TransactionStatus.Processing, out var processing);
        counts.TryGetValue(TransactionStatus.Completed,  out var completed);
        counts.TryGetValue(TransactionStatus.Deleted,    out var deleted);

        return new TransactionStatisticsDto(waiting, processing, completed, deleted,
            waiting + processing + completed + deleted);
    }
}
