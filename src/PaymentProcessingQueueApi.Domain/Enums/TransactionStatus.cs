namespace PaymentProcessingQueueApi.Domain.Enums;

/// <summary>Estado atual de uma transação dentro da fila de processamento.</summary>
public enum TransactionStatus
{
    /// <summary>Aguardando processamento na fila de prioridade.</summary>
    Waiting = 0,

    /// <summary>Selecionada e em processamento.</summary>
    Processing = 1,

    /// <summary>Liquidada/concluída com sucesso.</summary>
    Completed = 2,

    /// <summary>Excluída logicamente: não participa mais da fila nem das consultas comuns.</summary>
    Deleted = 3
}
