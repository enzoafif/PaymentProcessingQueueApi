namespace PaymentProcessingQueueApi.Domain.Enums;

/// <summary>Nível de risco antifraude apurado para a transação (entrada do motor antifraude).</summary>
public enum FraudRiskLevel
{
    /// <summary>Risco baixo: segue o fluxo normal de priorização.</summary>
    Low = 0,

    /// <summary>Risco médio: leve penalização na prioridade.</summary>
    Medium = 1,

    /// <summary>Risco alto: penalização forte — não deve "furar a fila" sem revisão.</summary>
    High = 2
}
