using PaymentProcessingQueueApi.Domain.DataStructures;
using PaymentProcessingQueueApi.Domain.Entities;

namespace PaymentProcessingQueueApi.Domain.Services;

/// <summary>
/// Fila de prioridade de transações apoiada em um <see cref="BinaryHeap{T}"/> (Heap).
///
/// É AQUI que o Heap se encaixa na solução: ele ordena as transações ativas por prioridade
/// (com desempate por data de criação) usando inserções O(log n) e extrações O(log n),
/// em vez de reordenar toda a coleção a cada operação.
/// </summary>
public sealed class TransactionPriorityQueue
{
    // Constrói um heap com as transações informadas. Cada Insert executa uma "subida" (sift-up).
    private static BinaryHeap<Transaction> Build(IEnumerable<Transaction> transactions)
    {
        var heap = new BinaryHeap<Transaction>(TransactionPriorityComparer.Instance);
        foreach (var transaction in transactions)
            heap.Insert(transaction);
        return heap;
    }

    /// <summary>
    /// Retorna a próxima transação a ser processada (topo do heap) sem removê-la,
    /// ou <c>null</c> se não houver transações ativas. Consulta do topo: O(1).
    /// </summary>
    public Transaction? Next(IEnumerable<Transaction> activeTransactions)
    {
        var heap = Build(activeTransactions);
        return heap.IsEmpty ? null : heap.Peek();
    }

    /// <summary>
    /// Calcula a posição de uma transação na fila (1 = próxima a ser atendida).
    /// A posição é "1 + (quantidade de transações com prioridade ESTRITAMENTE maior)",
    /// segundo o mesmo critério do heap. Roda em O(n) com O(1) de memória extra — não precisa
    /// reconstruir nem drenar o heap só para contar a posição.
    /// Retorna <c>null</c> se a transação não estiver entre as ativas.
    /// </summary>
    public int? PositionInQueue(Guid transactionId, IEnumerable<Transaction> activeTransactions)
    {
        // Materializa uma única vez para permitir duas passagens sem reenumerar a consulta.
        var list = activeTransactions as IReadOnlyList<Transaction> ?? activeTransactions.ToList();

        var target = list.FirstOrDefault(t => t.Id == transactionId);
        if (target is null)
            return null;

        var ahead = list.Count(t => t.Id != target.Id
            && TransactionPriorityComparer.Instance.Compare(t, target) > 0);

        return ahead + 1;
    }
}
