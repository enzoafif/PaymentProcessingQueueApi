namespace PaymentProcessingQueueApi.Domain.DataStructures;

/// <summary>
/// Heap binário (árvore binária quase completa armazenada em um vetor) usado para
/// implementar a fila de prioridade.
///
/// Propriedade do heap: cada nó tem prioridade maior ou igual à de seus filhos, de modo
/// que o elemento de MAIOR prioridade — segundo o <see cref="IComparer{T}"/> fornecido —
/// fica sempre na raiz (índice 0).
///
/// Mapeamento pai/filhos em vetor:
///   • filho esquerdo de i  → 2*i + 1
///   • filho direito de i    → 2*i + 2
///   • pai de i              → (i - 1) / 2
///
/// Complexidade:
///   • Peek (consultar topo)     → O(1)
///   • Insert (subida/sift-up)   → O(log n)
///   • ExtractTop (descida)      → O(log n)
///
/// Convenção do comparador: <c>Compare(a, b) &gt; 0</c> significa que "a" tem prioridade
/// MAIOR que "b" e, portanto, deve ficar mais próximo da raiz.
/// </summary>
public sealed class BinaryHeap<T>
{
    private readonly List<T> _items = [];
    private readonly IComparer<T> _comparer;

    public BinaryHeap(IComparer<T> comparer)
        => _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));

    /// <summary>Quantidade de itens no heap.</summary>
    public int Count => _items.Count;

    /// <summary>Indica se o heap está vazio.</summary>
    public bool IsEmpty => _items.Count == 0;

    /// <summary>Insere um item e o reposiciona "subindo" até restaurar a propriedade do heap. O(log n).</summary>
    public void Insert(T item)
    {
        _items.Add(item);
        SiftUp(_items.Count - 1);
    }

    /// <summary>Retorna o item de maior prioridade SEM removê-lo (raiz). O(1).</summary>
    public T Peek()
    {
        if (IsEmpty)
            throw new InvalidOperationException("A fila de prioridade está vazia.");
        return _items[0];
    }

    /// <summary>Remove e retorna o item de maior prioridade, "descendo" o novo topo. O(log n).</summary>
    public T ExtractTop()
    {
        if (IsEmpty)
            throw new InvalidOperationException("A fila de prioridade está vazia.");

        var top = _items[0];
        var last = _items.Count - 1;

        _items[0] = _items[last];   // move o último elemento para a raiz
        _items.RemoveAt(last);

        if (!IsEmpty)
            SiftDown(0);            // restaura a propriedade do heap descendo a raiz

        return top;
    }

    // Subida (sift-up): enquanto o nó tiver prioridade maior que a do pai, troca com o pai.
    private void SiftUp(int index)
    {
        while (index > 0)
        {
            var parent = (index - 1) / 2;
            if (_comparer.Compare(_items[index], _items[parent]) <= 0)
                break; // já está na posição correta
            Swap(index, parent);
            index = parent;
        }
    }

    // Descida (sift-down): enquanto algum filho tiver prioridade maior, troca com o maior filho.
    private void SiftDown(int index)
    {
        var size = _items.Count;
        while (true)
        {
            var left = 2 * index + 1;
            var right = 2 * index + 2;
            var largest = index;

            if (left < size && _comparer.Compare(_items[left], _items[largest]) > 0)
                largest = left;
            if (right < size && _comparer.Compare(_items[right], _items[largest]) > 0)
                largest = right;

            if (largest == index)
                break; // propriedade do heap restaurada
            Swap(index, largest);
            index = largest;
        }
    }

    /// <summary>
    /// Retorna os itens na ordem interna do vetor do heap (índice = posição na árvore binária).
    /// Índice 0 = raiz; filho esquerdo de i = 2i+1; filho direito de i = 2i+2.
    /// </summary>
    public IReadOnlyList<T> ToHeapArray() => _items.AsReadOnly();

    /// <summary>Descreve o papel de um nó pelo seu índice no vetor do heap.</summary>
    public static string RoleLabel(int index) => index switch
    {
        0 => "Raiz",
        _ when index % 2 == 1 => $"Filho Esquerdo (pai: índice {(index - 1) / 2})",
        _ => $"Filho Direito (pai: índice {(index - 1) / 2})"
    };

    private void Swap(int a, int b)
        => (_items[a], _items[b]) = (_items[b], _items[a]);
}
