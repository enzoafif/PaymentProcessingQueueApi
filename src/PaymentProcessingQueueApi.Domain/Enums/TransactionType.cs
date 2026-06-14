namespace PaymentProcessingQueueApi.Domain.Enums;

/// <summary>Tipo da transação financeira (influencia a janela de liquidação e, portanto, a prioridade).</summary>
public enum TransactionType
{
    /// <summary>Pagamento instantâneo via Pix.</summary>
    Pix = 0,

    /// <summary>Pagamento com cartão de crédito.</summary>
    Credito = 1,

    /// <summary>Liquidação de boleto bancário.</summary>
    Boleto = 2,

    /// <summary>Pagamento com cartão de débito.</summary>
    Debito = 3
}
