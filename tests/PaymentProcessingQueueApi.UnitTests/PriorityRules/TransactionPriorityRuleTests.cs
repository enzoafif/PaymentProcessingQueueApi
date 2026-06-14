using PaymentProcessingQueueApi.Domain.Entities;
using PaymentProcessingQueueApi.Domain.Enums;
using PaymentProcessingQueueApi.Domain.PriorityRules;
using PaymentProcessingQueueApi.Domain.Services;
using Xunit;

namespace PaymentProcessingQueueApi.UnitTests.PriorityRules;

public class TransactionPriorityRuleTests
{
    private static readonly TransactionPriorityRule Rule = new();
    private static readonly DateTime BaseTime = new(2026, 1, 1, 12, 0, 0);

    private static Transaction Make(
        decimal amount,
        TransactionType type = TransactionType.Pix,
        ClientType clientType = ClientType.Standard,
        FraudRiskLevel fraud = FraudRiskLevel.Low,
        double cutoffOffsetMinutes = 300)
    {
        var cutoff = BaseTime.AddMinutes(cutoffOffsetMinutes);
        return Transaction.Create("11144477735", "Test", null, amount, type, clientType, fraud, cutoff, BaseTime);
    }

    // ─── Valor ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Amount_AboveOneMillion_Scores40()
    {
        var t = Make(1_500_000m);
        var result = Rule.Calculate(t, BaseTime);
        Assert.Equal(40, result.Components.First(c => c.Factor == "Amount").Points);
    }

    [Fact]
    public void Amount_AboveHundredThousand_Scores30()
    {
        var t = Make(250_000m);
        var result = Rule.Calculate(t, BaseTime);
        Assert.Equal(30, result.Components.First(c => c.Factor == "Amount").Points);
    }

    [Fact]
    public void Amount_BelowOneThousand_Scores5()
    {
        var t = Make(500m);
        var result = Rule.Calculate(t, BaseTime);
        Assert.Equal(5, result.Components.First(c => c.Factor == "Amount").Points);
    }

    // ─── Cutoff ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Cutoff_Expired_Scores35()
    {
        var t = Make(1000m, cutoffOffsetMinutes: -5);
        var result = Rule.Calculate(t, BaseTime);
        Assert.Equal(35, result.Components.First(c => c.Factor == "CutoffTime").Points);
    }

    [Fact]
    public void Cutoff_Within15Min_Scores30()
    {
        var t = Make(1000m, cutoffOffsetMinutes: 10);
        var result = Rule.Calculate(t, BaseTime);
        Assert.Equal(30, result.Components.First(c => c.Factor == "CutoffTime").Points);
    }

    [Fact]
    public void Cutoff_Beyond4Hours_Scores3()
    {
        var t = Make(1000m, cutoffOffsetMinutes: 300);
        var result = Rule.Calculate(t, BaseTime);
        Assert.Equal(3, result.Components.First(c => c.Factor == "CutoffTime").Points);
    }

    // ─── Tipo de cliente ─────────────────────────────────────────────────────────

    [Fact]
    public void ClientType_Corporate_Scores15()
    {
        var t = Make(1000m, clientType: ClientType.Corporate);
        var result = Rule.Calculate(t, BaseTime);
        Assert.Equal(15, result.Components.First(c => c.Factor == "ClientType").Points);
    }

    [Fact]
    public void ClientType_Standard_Scores3()
    {
        var t = Make(1000m, clientType: ClientType.Standard);
        var result = Rule.Calculate(t, BaseTime);
        Assert.Equal(3, result.Components.First(c => c.Factor == "ClientType").Points);
    }

    // ─── Risco antifraude (penalidade) ───────────────────────────────────────────

    [Fact]
    public void FraudRisk_High_AppliesMinus20()
    {
        var t = Make(1000m, fraud: FraudRiskLevel.High);
        var result = Rule.Calculate(t, BaseTime);
        Assert.Equal(-20, result.Components.First(c => c.Factor == "FraudRisk").Points);
    }

    [Fact]
    public void FraudRisk_High_NeverProducesNegativeTotal()
    {
        // Pontuação mínima sem fraude: 5+3+3+3 = 14; com High: 14-20 = -6 => deve ser 0
        var t = Make(500m, TransactionType.Boleto, ClientType.Standard, FraudRiskLevel.High, cutoffOffsetMinutes: 300);
        var result = Rule.Calculate(t, BaseTime);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public void FraudRisk_Medium_AppliesMinus5()
    {
        var t = Make(1000m, fraud: FraudRiskLevel.Medium);
        var result = Rule.Calculate(t, BaseTime);
        Assert.Equal(-5, result.Components.First(c => c.Factor == "FraudRisk").Points);
    }

    // ─── Cálculo total ────────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_AlwaysReturns5Components()
    {
        var t = Make(50_000m);
        var result = Rule.Calculate(t, BaseTime);
        Assert.Equal(5, result.Components.Count);
    }

    [Fact]
    public void Calculate_MaxScenario_ReturnsExpectedTotal()
    {
        // Amount>=1M(40) + Cutoff<=15min(30) + Corporate(15) + InternationalRemittance(10) + Low(0) = 95
        var t = Make(2_000_000m, TransactionType.InternationalRemittance, ClientType.Corporate,
            FraudRiskLevel.Low, cutoffOffsetMinutes: 5);
        var result = Rule.Calculate(t, BaseTime);
        Assert.Equal(95, result.Total);
    }

    // ─── Desempate ────────────────────────────────────────────────────────────────

    [Fact]
    public void Tiebreak_OlderTransaction_ComesFirst()
    {
        var t1 = Transaction.Create("11144477735", "Antiga", null, 10_000m,
            TransactionType.Pix, ClientType.Premium, FraudRiskLevel.Low,
            BaseTime.AddHours(2), BaseTime);

        var t2 = Transaction.Create("52998224725", "Recente", null, 10_000m,
            TransactionType.Pix, ClientType.Premium, FraudRiskLevel.Low,
            BaseTime.AddHours(2), BaseTime.AddSeconds(10));

        var priority1 = Rule.Calculate(t1, BaseTime).Total;
        var priority2 = Rule.Calculate(t2, BaseTime).Total;

        t1.AssignPriority(priority1);
        t2.AssignPriority(priority2);

        Assert.Equal(t1.Priority, t2.Priority); // mesma prioridade

        // t1 é mais antiga => comparer deve colocá-la primeiro (retorno positivo = maior)
        Assert.True(TransactionPriorityComparer.Instance.Compare(t1, t2) > 0);
    }
}
