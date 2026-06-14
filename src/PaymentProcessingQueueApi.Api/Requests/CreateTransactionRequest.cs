using System.ComponentModel.DataAnnotations;
using PaymentProcessingQueueApi.Api.Validation;
using PaymentProcessingQueueApi.Domain.Enums;

namespace PaymentProcessingQueueApi.Api.Requests;

/// <summary>Dados de entrada para cadastrar uma transação na fila de processamento.</summary>
public sealed class CreateTransactionRequest
{
    /// <summary>CPF (11 dígitos) ou CNPJ (14 dígitos) válido do cliente, apenas números.</summary>
    /// <example>11144477735</example>
    [Required]
    [CpfCnpj]
    public string Cpf { get; set; } = string.Empty;

    /// <summary>Descrição da transação.</summary>
    /// <example>PIX agendado folha de pagamento</example>
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Referência externa da transação (opcional).</summary>
    /// <example>PIX-2026-001</example>
    [StringLength(100)]
    public string? Reference { get; set; }

    /// <summary>Valor da transação. Deve ser maior que zero.</summary>
    /// <example>120000.00</example>
    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Amount { get; set; }

    /// <summary>Tipo da transação: Pix, Credito, Debito ou Boleto.</summary>
    /// <example>Pix</example>
    [Required]
    [EnumDataType(typeof(TransactionType))]
    public TransactionType? Type { get; set; }

    /// <summary>Segmento do cliente: Standard, Premium ou Corporate.</summary>
    /// <example>Premium</example>
    [Required]
    [EnumDataType(typeof(ClientType))]
    public ClientType? ClientType { get; set; }

    /// <summary>Risco antifraude: Low, Medium ou High.</summary>
    /// <example>Low</example>
    [Required]
    [EnumDataType(typeof(FraudRiskLevel))]
    public FraudRiskLevel? FraudRisk { get; set; }

    /// <summary>Horário limite (cutoff) para liquidação da transação.</summary>
    /// <example>2026-06-11T18:00:00</example>
    [Required]
    public DateTime? CutoffTime { get; set; }
}
