using PaymentProcessingQueueApi.Domain.DataStructures;
using Xunit;

namespace PaymentProcessingQueueApi.UnitTests.DataStructures;

public class BinaryHeapTests
{
    private static BinaryHeap<int> NewHeap() => new(Comparer<int>.Default);

    // ─── Estado inicial ───────────────────────────────────────────────────────────

    [Fact]
    public void Initially_IsEmptyAndCountZero()
    {
        var heap = NewHeap();
        Assert.True(heap.IsEmpty);
        Assert.Equal(0, heap.Count);
    }

    // ─── Peek ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Peek_EmptyHeap_Throws()
        => Assert.Throws<InvalidOperationException>(() => NewHeap().Peek());

    [Fact]
    public void Peek_AfterInserts_ReturnsMax()
    {
        var heap = NewHeap();
        heap.Insert(3); heap.Insert(1); heap.Insert(5); heap.Insert(2);
        Assert.Equal(5, heap.Peek());
        Assert.Equal(4, heap.Count); // peek não remove
    }

    // ─── Insert ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Insert_Single_CountIsOne()
    {
        var heap = NewHeap();
        heap.Insert(42);
        Assert.Equal(1, heap.Count);
        Assert.False(heap.IsEmpty);
    }

    [Fact]
    public void Insert_DescendingOrder_HeapPropertyMaintained()
    {
        var heap = NewHeap();
        foreach (var v in new[] { 9, 7, 5, 3, 1 }) heap.Insert(v);
        Assert.Equal(9, heap.Peek());
    }

    [Fact]
    public void Insert_AscendingOrder_HeapPropertyMaintained()
    {
        var heap = NewHeap();
        foreach (var v in new[] { 1, 3, 5, 7, 9 }) heap.Insert(v);
        Assert.Equal(9, heap.Peek());
    }

    // ─── ExtractTop ───────────────────────────────────────────────────────────────

    [Fact]
    public void ExtractTop_EmptyHeap_Throws()
        => Assert.Throws<InvalidOperationException>(() => NewHeap().ExtractTop());

    [Fact]
    public void ExtractTop_ReturnsMaxAndReducesCount()
    {
        var heap = NewHeap();
        heap.Insert(3); heap.Insert(1); heap.Insert(5);
        Assert.Equal(5, heap.ExtractTop());
        Assert.Equal(2, heap.Count);
    }

    [Fact]
    public void ExtractTop_AllElements_ReturnsDescendingOrder()
    {
        var heap = NewHeap();
        foreach (var v in new[] { 4, 2, 7, 1, 9, 3 }) heap.Insert(v);

        var extracted = new List<int>();
        while (!heap.IsEmpty) extracted.Add(heap.ExtractTop());

        Assert.Equal(new[] { 9, 7, 4, 3, 2, 1 }, extracted);
    }

    [Fact]
    public void ExtractTop_SingleElement_LeavesHeapEmpty()
    {
        var heap = NewHeap();
        heap.Insert(10);
        heap.ExtractTop();
        Assert.True(heap.IsEmpty);
    }

    // ─── Duplicatas ───────────────────────────────────────────────────────────────

    [Fact]
    public void Insert_Duplicates_HandledCorrectly()
    {
        var heap = NewHeap();
        heap.Insert(5); heap.Insert(5); heap.Insert(5);
        Assert.Equal(3, heap.Count);
        Assert.Equal(5, heap.ExtractTop());
        Assert.Equal(5, heap.ExtractTop());
        Assert.Equal(5, heap.ExtractTop());
        Assert.True(heap.IsEmpty);
    }
}
