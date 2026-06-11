using PaymentProcessingQueueApi.Domain.Entities;
using PaymentProcessingQueueApi.Domain.Enums;

namespace PaymentProcessingQueueApi.Domain.PriorityRules;

/// <summary>
/// Regra de prioridade do cenário de processamento de pagamentos (item 12.10).
///
/// A prioridade é DERIVADA dos dados de domínio (não é digitada pelo usuário) por meio
/// de uma tabela de decisão. Quanto maior a pontuação, mais cedo a transação deve ser
/// executada, evitando multas contratuais e garantindo liquidez ao sistema financeiro.
///
/// Fatores (conforme o enunciado): valor da transação, horário limite (cutoff),
/// tipo de cliente, tipo de transação e risco antifraude.
/// Faixa teórica do total: 0 (mínimo) a 100 (máximo, antes de penalidades de risco).
/// </summary>
public sealed class TransactionPriorityRule : IPriorityRule
{
    public PriorityResult Calculate(Transaction transaction, DateTime reference)
    {
        var components = new List<PriorityComponent>
        {
            ScoreAmount(transaction.Amount),
            ScoreCutoff(transaction.CutoffTime, reference),
            ScoreClientType(transaction.ClientType),
            ScoreTransactionType(transaction.Type),
            ScoreFraudRisk(transaction.FraudRisk)
        };

        var total = components.Sum(c => c.Points);
        if (total < 0) total = 0; // a prioridade mínima é zero

        return new PriorityResult(total, components);
    }

    // Valor: maior valor => maior exposição a multas/liquidez => maior prioridade (5..40).
    private static PriorityComponent ScoreAmount(decimal amount)
    {
        var (points, range) = amount switch
        {
            >= 1_000_000m => (40, "≥ R$ 1.000.000"),
            >= 100_000m   => (30, "≥ R$ 100.000"),
            >= 10_000m    => (20, "≥ R$ 10.000"),
            >= 1_000m     => (10, "≥ R$ 1.000"),
            _             => (5,  "< R$ 1.000")
        };
        return new PriorityComponent("Amount", points, $"Valor da transação ({range}).");
    }

    // Cutoff: quanto mais próximo (ou já vencido) o horário limite, maior a urgência (3..35).
    private static PriorityComponent ScoreCutoff(DateTime cutoff, DateTime reference)
    {
        var minutes = (cutoff - reference).TotalMinutes;
        var (points, range) = minutes switch
        {
            <= 0d   => (35, "cutoff já vencido"),
            <= 15d  => (30, "≤ 15 min do cutoff"),
            <= 60d  => (20, "≤ 1 h do cutoff"),
            <= 240d => (10, "≤ 4 h do cutoff"),
            _       => (3,  "> 4 h do cutoff")
        };
        return new PriorityComponent("CutoffTime", points, $"Proximidade do horário limite ({range}).");
    }

    // Tipo de cliente: SLA contratual (3..15).
    private static PriorityComponent ScoreClientType(ClientType type)
    {
        var points = type switch
        {
            ClientType.Corporate => 15,
            ClientType.Premium   => 10,
            _                    => 3
        };
        return new PriorityComponent("ClientType", points, $"Segmento do cliente ({type}).");
    }

    // Tipo de transação: janelas de liquidação mais rígidas pontuam mais (3..10).
    private static PriorityComponent ScoreTransactionType(TransactionType type)
    {
        var points = type switch
        {
            TransactionType.InternationalRemittance => 10,
            TransactionType.Pix                     => 8,
            TransactionType.Ted                     => 5,
            _                                       => 3
        };
        return new PriorityComponent("TransactionType", points, $"Tipo de transação ({type}).");
    }

    // Risco antifraude: transações de alto risco NÃO devem furar a fila sem revisão (penalidade).
    private static PriorityComponent ScoreFraudRisk(FraudRiskLevel risk)
    {
        var points = risk switch
        {
            FraudRiskLevel.High   => -20,
            FraudRiskLevel.Medium => -5,
            _                     => 0
        };
        return new PriorityComponent("FraudRisk", points, $"Ajuste por risco antifraude ({risk}).");
    }
}
