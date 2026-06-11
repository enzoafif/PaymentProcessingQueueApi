using PaymentProcessingQueueApi.Domain.Entities;

namespace PaymentProcessingQueueApi.Domain.PriorityRules;

/// <summary>
/// Contrato da regra de prioridade. Mantido na camada de Domínio para que possa ser
/// lido, testado e explicado isoladamente (princípio aberto/fechado: novas regras
/// podem ser criadas sem alterar quem a consome).
/// </summary>
public interface IPriorityRule
{
    /// <summary>
    /// Calcula a prioridade de uma transação a partir dos seus dados de domínio.
    /// </summary>
    /// <param name="transaction">Transação a ser avaliada.</param>
    /// <param name="reference">Instante de referência (usado para medir a proximidade do cutoff).</param>
    PriorityResult Calculate(Transaction transaction, DateTime reference);
}
