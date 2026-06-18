namespace PaymentProcessingQueueApi.Api.Responses;

/// <summary>Detalhe de uma parcela do cálculo da prioridade.</summary>
public sealed class PriorityComponentResponse
{
    /// <summary>Fator avaliado (ex.: "Amount", "CutoffTime").</summary>
    public string Factor { get; init; } = string.Empty;

    /// <summary>Pontuação atribuída pelo fator.</summary>
    public int Points { get; init; }

    /// <summary>Explicação legível da pontuação.</summary>
    public string Reason { get; init; } = string.Empty;
}

/// <summary>Representação de uma transação retornada pela API.</summary>
public sealed class TransactionResponse
{
    public Guid Id { get; init; }
    public string Cpf { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Reference { get; init; }
    public decimal Amount { get; init; }
    public string Type { get; init; } = string.Empty;
    public string ClientType { get; init; } = string.Empty;
    public string FraudRisk { get; init; } = string.Empty;
    public DateTime CutoffTime { get; init; }

    /// <summary>Prioridade calculada (quanto maior, mais cedo a transação deve ser processada).</summary>
    public int Priority { get; init; }

    public string Status { get; init; } = string.Empty;
    public bool Active { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }

    /// <summary>Posição atual na fila de prioridade (1 = próxima a ser processada).</summary>
    public int? PositionInQueue { get; init; }

    /// <summary>Detalhamento de como a prioridade foi calculada.</summary>
    public IReadOnlyList<PriorityComponentResponse> PriorityComponents { get; init; } = [];

    /// <summary>
    /// Índice da transação no vetor interno do heap (null se não estiver na fila ativa).
    /// Índice 0 = raiz; filho esquerdo de i = 2i+1; filho direito de i = 2i+2.
    /// </summary>
    public int? HeapIndex { get; init; }

    /// <summary>Papel da transação na árvore do heap (ex.: "Raiz", "Filho Esquerdo (pai: índice 0)").</summary>
    public string? HeapRole { get; init; }
}
