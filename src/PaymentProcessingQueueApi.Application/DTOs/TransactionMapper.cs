using PaymentProcessingQueueApi.Domain.DataStructures;
using PaymentProcessingQueueApi.Domain.Entities;
using PaymentProcessingQueueApi.Domain.PriorityRules;

namespace PaymentProcessingQueueApi.Application.DTOs;

/// <summary>Converte a entidade de domínio em <see cref="TransactionDto"/>.</summary>
internal static class TransactionMapper
{
    public static TransactionDto ToDto(
        Transaction transaction,
        int? positionInQueue,
        PriorityResult priority,
        int? heapIndex = null)
    {
        var components = priority.Components
            .Select(c => new PriorityComponentDto(c.Factor, c.Points, c.Reason))
            .ToList();

        return new TransactionDto(
            transaction.Id,
            transaction.Cpf,
            transaction.Description,
            transaction.Reference,
            transaction.Amount,
            transaction.Type.ToString(),
            transaction.ClientType.ToString(),
            transaction.FraudRisk.ToString(),
            transaction.CutoffTime,
            transaction.Priority,
            transaction.Status.ToString(),
            transaction.Active,
            transaction.CreatedAt,
            transaction.UpdatedAt,
            transaction.DeletedAt,
            positionInQueue,
            components,
            heapIndex,
            heapIndex.HasValue ? BinaryHeap<Transaction>.RoleLabel(heapIndex.Value) : null);
    }
}
