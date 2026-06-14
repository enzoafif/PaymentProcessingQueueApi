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

    // Valor: dois grupos separados em R$ 5.000.
    // Alto valor (> 5k): 25–50 pontos proporcionais ao valor (cap em R$ 1.000.000).
    // Baixo valor (≤ 5k): 0–24 pontos proporcionais ao valor.
    // Dentro de cada grupo, quanto maior o valor maior a pontuação.
    private static PriorityComponent ScoreAmount(decimal amount)
    {
        int points;
        string group;

        if (amount > 5_000m)
        {
            var normalized = Math.Min(1m, (amount - 5_000m) / 995_000m);
            points = 25 + (int)Math.Round(normalized * 25m);
            group = "Alto valor";
        }
        else
        {
            var normalized = amount / 5_000m;
            points = (int)Math.Round(normalized * 24m);
            group = "Baixo valor";
        }

        return new PriorityComponent("Amount", points, $"Valor da transação ({group}: R$ {amount:N2}).");
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

    // Tipo de transação: liquidações mais rígidas pontuam mais (3..8).
    private static PriorityComponent ScoreTransactionType(TransactionType type)
    {
        var points = type switch
        {
            TransactionType.Pix     => 8,
            TransactionType.Credito => 6,
            TransactionType.Debito  => 5,
            _                       => 3  // Boleto
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
