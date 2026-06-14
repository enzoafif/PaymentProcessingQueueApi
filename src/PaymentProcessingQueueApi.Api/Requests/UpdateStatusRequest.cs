using System.ComponentModel.DataAnnotations;
using PaymentProcessingQueueApi.Domain.Enums;

namespace PaymentProcessingQueueApi.Api.Requests;

/// <summary>
/// Dados de entrada para atualizar apenas o status de uma transação.
/// Não aceita o status Deleted — para isso use o endpoint DELETE.
/// </summary>
public sealed class UpdateStatusRequest
{
    /// <summary>Novo status: Waiting, Processing ou Completed.</summary>
    /// <example>Completed</example>
    [Required]
    [EnumDataType(typeof(TransactionStatus))]
    public TransactionStatus? Status { get; set; }
}
