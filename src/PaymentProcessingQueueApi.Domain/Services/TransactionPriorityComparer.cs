using PaymentProcessingQueueApi.Domain.Entities;

namespace PaymentProcessingQueueApi.Domain.Services;

/// <summary>
/// Define a ordem de atendimento da fila de prioridade:
///   1) maior <see cref="Transaction.Priority"/> primeiro;
///   2) em caso de EMPATE, a transação criada há mais tempo (menor <see cref="Transaction.CreatedAt"/>)
///      é atendida antes — critério de desempate exigido pelo enunciado (FIFO no empate).
///
/// Convenção (compatível com <see cref="DataStructures.BinaryHeap{T}"/>): retorno positivo
/// significa que "x" tem prioridade maior que "y" e deve ficar mais próximo da raiz do heap.
/// </summary>
public sealed class TransactionPriorityComparer : IComparer<Transaction>
{
    /// <summary>Instância compartilhada (o comparador não tem estado).</summary>
    public static TransactionPriorityComparer Instance { get; } = new();

    public int Compare(Transaction? x, Transaction? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        // 1) maior prioridade fica mais perto da raiz
        var byPriority = x.Priority.CompareTo(y.Priority);
        if (byPriority != 0)
            return byPriority;

        // 2) desempate: mais antigo tem preferência => deve ser "maior" no heap.
        //    Como CreatedAt menor deve vencer, invertemos a comparação.
        return y.CreatedAt.CompareTo(x.CreatedAt);
    }
}
