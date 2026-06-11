using PaymentProcessingQueueApi.Domain.Enums;

namespace PaymentProcessingQueueApi.Application.UseCases.CreateTransaction;

/// <summary>
/// Dados de entrada (já validados de formato pela API) para cadastrar uma transação.
/// Observe que NÃO existe campo "prioridade": ela é calculada pelo domínio.
/// </summary>
public sealed record CreateTransactionCommand(
    string Cpf,
    string Description,
    string? Reference,
    decimal Amount,
    TransactionType Type,
    ClientType ClientType,
    FraudRiskLevel FraudRisk,
    DateTime CutoffTime);
