namespace PaymentProcessingQueueApi.Domain.PriorityRules;

/// <summary>Parcela individual que compõe o cálculo da prioridade (para transparência/explicação).</summary>
/// <param name="Factor">Nome do fator avaliado (ex.: "Amount").</param>
/// <param name="Points">Pontuação atribuída por esse fator.</param>
/// <param name="Reason">Explicação legível da pontuação.</param>
public sealed record PriorityComponent(string Factor, int Points, string Reason);

/// <summary>Resultado do cálculo de prioridade: total e detalhamento por fator.</summary>
/// <param name="Total">Prioridade final (soma das parcelas, nunca negativa).</param>
/// <param name="Components">Parcelas que compuseram o total.</param>
public sealed record PriorityResult(int Total, IReadOnlyList<PriorityComponent> Components);
