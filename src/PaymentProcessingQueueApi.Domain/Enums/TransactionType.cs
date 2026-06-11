namespace PaymentProcessingQueueApi.Domain.Enums;

/// <summary>Tipo da transação financeira (influencia a janela de liquidação e, portanto, a prioridade).</summary>
public enum TransactionType
{
    /// <summary>Pagamento instantâneo.</summary>
    Pix = 0,

    /// <summary>Transferência Eletrônica Disponível (TED).</summary>
    Ted = 1,

    /// <summary>Liquidação de boleto.</summary>
    Boleto = 2,

    /// <summary>Remessa internacional (janela de câmbio rígida).</summary>
    InternationalRemittance = 3
}
