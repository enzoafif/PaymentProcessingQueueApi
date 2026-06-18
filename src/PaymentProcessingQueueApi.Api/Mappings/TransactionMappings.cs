using PaymentProcessingQueueApi.Api.Responses;
using PaymentProcessingQueueApi.Application.DTOs;

namespace PaymentProcessingQueueApi.Api.Mappings;

/// <summary>Converte os DTOs da Aplicação nos contratos de saída (Responses) da API.</summary>
public static class TransactionMappings
{
    public static TransactionResponse ToResponse(this TransactionDto dto) => new()
    {
        Id = dto.Id,
        Cpf = dto.Cpf,
        Description = dto.Description,
        Reference = dto.Reference,
        Amount = dto.Amount,
        Type = dto.Type,
        ClientType = dto.ClientType,
        FraudRisk = dto.FraudRisk,
        CutoffTime = dto.CutoffTime,
        Priority = dto.Priority,
        Status = dto.Status,
        Active = dto.Active,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt,
        DeletedAt = dto.DeletedAt,
        PositionInQueue = dto.PositionInQueue,
        PriorityComponents = dto.PriorityComponents
            .Select(c => new PriorityComponentResponse { Factor = c.Factor, Points = c.Points, Reason = c.Reason })
            .ToList(),
        HeapIndex = dto.HeapIndex,
        HeapRole = dto.HeapRole
    };

    public static PagedTransactionResponse ToPagedResponse(this PagedResultDto<TransactionDto> paged) => new()
    {
        Items = paged.Items.Select(d => d.ToResponse()).ToList(),
        TotalItems = paged.TotalItems,
        TotalPages = paged.TotalPages,
        CurrentPage = paged.CurrentPage,
        PageSize = paged.PageSize
    };

    public static StatisticsResponse ToResponse(this TransactionStatisticsDto dto) => new()
    {
        Waiting    = dto.Waiting,
        Processing = dto.Processing,
        Completed  = dto.Completed,
        Deleted    = dto.Deleted,
        Total      = dto.Total
    };
}
