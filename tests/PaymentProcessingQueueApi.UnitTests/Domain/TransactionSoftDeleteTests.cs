using PaymentProcessingQueueApi.Domain.Entities;
using PaymentProcessingQueueApi.Domain.Enums;
using PaymentProcessingQueueApi.Domain.Exceptions;
using Xunit;

namespace PaymentProcessingQueueApi.UnitTests.Domain;

public class TransactionSoftDeleteTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 10, 0, 0);

    private static Transaction CreateTransaction()
        => Transaction.Create(
            "11144477735", "Transação de teste", null, 5_000m,
            TransactionType.Pix, ClientType.Standard, FraudRiskLevel.Low,
            Now.AddHours(2), Now);

    // ─── Estado inicial ───────────────────────────────────────────────────────────

    [Fact]
    public void Create_TransactionIsActiveAndWaiting()
    {
        var t = CreateTransaction();
        Assert.True(t.Active);
        Assert.Equal(TransactionStatus.Waiting, t.Status);
        Assert.Null(t.DeletedAt);
    }

    // ─── SoftDelete ───────────────────────────────────────────────────────────────

    [Fact]
    public void SoftDelete_SetsStatusDeletedAndActiveFalse()
    {
        var t = CreateTransaction();
        t.SoftDelete(Now);

        Assert.Equal(TransactionStatus.Deleted, t.Status);
        Assert.False(t.Active);
        Assert.Equal(Now, t.DeletedAt);
        Assert.Equal(Now, t.UpdatedAt);
    }

    [Fact]
    public void SoftDelete_Twice_ThrowsBusinessRuleException()
    {
        var t = CreateTransaction();
        t.SoftDelete(Now);

        Assert.Throws<BusinessRuleException>(() => t.SoftDelete(Now.AddMinutes(1)));
    }

    [Fact]
    public void SoftDeleted_TransactionDoesNotAppearInActiveQuery()
    {
        var t = CreateTransaction();
        t.SoftDelete(Now);

        // Simula o filtro do repositório: Active && Status != Deleted
        var visibleInActive = t.Active && t.Status != TransactionStatus.Deleted;
        Assert.False(visibleInActive);
    }

    // ─── Update de entidade excluída ─────────────────────────────────────────────

    [Fact]
    public void Update_OnDeletedTransaction_ThrowsBusinessRuleException()
    {
        var t = CreateTransaction();
        t.SoftDelete(Now);

        Assert.Throws<BusinessRuleException>(() =>
            t.Update("Nova desc", null, 1000m, TransactionType.Ted,
                ClientType.Premium, FraudRiskLevel.Low, Now.AddHours(3), Now));
    }

    // ─── UpdateStatus ─────────────────────────────────────────────────────────────

    [Fact]
    public void UpdateStatus_ToProcessing_ChangesStatusAndSetsUpdatedAt()
    {
        var t = CreateTransaction();
        t.UpdateStatus(TransactionStatus.Processing, Now);

        Assert.Equal(TransactionStatus.Processing, t.Status);
        Assert.Equal(Now, t.UpdatedAt);
    }

    [Fact]
    public void UpdateStatus_ToDeleted_ThrowsBusinessRuleException()
    {
        var t = CreateTransaction();
        Assert.Throws<BusinessRuleException>(() => t.UpdateStatus(TransactionStatus.Deleted, Now));
    }

    [Fact]
    public void UpdateStatus_OnDeletedTransaction_ThrowsBusinessRuleException()
    {
        var t = CreateTransaction();
        t.SoftDelete(Now);

        Assert.Throws<BusinessRuleException>(() => t.UpdateStatus(TransactionStatus.Waiting, Now));
    }
}
