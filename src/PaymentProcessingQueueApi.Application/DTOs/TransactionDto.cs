namespace PaymentProcessingQueueApi.Application.DTOs;

/// <summary>Parcela do cálculo da prioridade exposta pela camada de Aplicação.</summary>
public sealed record PriorityComponentDto(string Factor, int Points, string Reason);

/// <summary>
/// Resultado de saída dos casos de uso de transação. Desacopla o contrato da Aplicação
/// da entidade de domínio (a entidade não "vaza" para fora da camada).
/// </summary>
public sealed record TransactionDto(
    Guid Id,
    string Cpf,
    string Description,
    string? Reference,
    decimal Amount,
    string Type,
    string ClientType,
    string FraudRisk,
    DateTime CutoffTime,
    int Priority,
    string Status,
    bool Active,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? DeletedAt,
    int? PositionInQueue,
    IReadOnlyList<PriorityComponentDto> PriorityComponents,
    int? HeapIndex,
    string? HeapRole);
