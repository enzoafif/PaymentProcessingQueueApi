using System.ComponentModel.DataAnnotations;
using PaymentProcessingQueueApi.Domain.Enums;

namespace PaymentProcessingQueueApi.Api.Requests;

/// <summary>Dados de entrada para atualizar uma transação existente.</summary>
public sealed class UpdateTransactionRequest
{
    /// <summary>Nova descrição da transação.</summary>
    /// <example>PIX urgente folha de pagamento</example>
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Nova referência externa (opcional).</summary>
    /// <example>PIX-2026-002</example>
    [StringLength(100)]
    public string? Reference { get; set; }

    /// <summary>Novo valor da transação. Deve ser maior que zero.</summary>
    /// <example>150000.00</example>
    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Amount { get; set; }

    /// <summary>Novo tipo da transação: Pix, Credito, Debito ou Boleto.</summary>
    /// <example>Pix</example>
    [Required]
    [EnumDataType(typeof(TransactionType))]
    public TransactionType? Type { get; set; }

    /// <summary>Novo segmento do cliente: Standard, Premium ou Corporate.</summary>
    /// <example>Corporate</example>
    [Required]
    [EnumDataType(typeof(ClientType))]
    public ClientType? ClientType { get; set; }

    /// <summary>Novo nível de risco antifraude: Low, Medium ou High.</summary>
    /// <example>Low</example>
    [Required]
    [EnumDataType(typeof(FraudRiskLevel))]
    public FraudRiskLevel? FraudRisk { get; set; }

    /// <summary>Novo horário limite (cutoff) para liquidação.</summary>
    /// <example>2026-06-11T20:00:00</example>
    [Required]
    public DateTime? CutoffTime { get; set; }
}
