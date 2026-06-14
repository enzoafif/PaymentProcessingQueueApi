using PaymentProcessingQueueApi.Domain.Enums;

namespace PaymentProcessingQueueApi.Application.UseCases.UpdateTransaction;

public record UpdateTransactionCommand(
    Guid Id,
    string Description,
    string? Reference,
    decimal Amount,
    TransactionType Type,
    ClientType ClientType,
    FraudRiskLevel FraudRisk,
    DateTime CutoffTime);
