using PaymentProcessingQueueApi.Domain.Enums;
using PaymentProcessingQueueApi.Domain.Exceptions;

namespace PaymentProcessingQueueApi.Domain.Entities;

/// <summary>
/// Entidade principal do cenário "Processamento de Pagamentos" (item 12.10).
/// Representa uma transação financeira que aguarda execução em uma fila de prioridade.
/// A prioridade NÃO é informada pelo usuário: ela é calculada/derivada dos dados da
/// transação (ver <c>PriorityRules</c>) e atribuída via <see cref="AssignPriority"/>.
/// </summary>
public class Transaction
{
    /// <summary>Identificador único da transação.</summary>
    public Guid Id { get; private set; }

    /// <summary>CPF/CNPJ do cliente (apenas dígitos).</summary>
    public string Cpf { get; private set; }

    /// <summary>Descrição da transação.</summary>
    public string Description { get; private set; }

    /// <summary>Referência externa (ex.: número da remessa). Opcional.</summary>
    public string? Reference { get; private set; }

    /// <summary>Valor financeiro da transação.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Tipo da transação (PIX, TED, boleto, remessa internacional).</summary>
    public TransactionType Type { get; private set; }

    /// <summary>Segmento do cliente.</summary>
    public ClientType ClientType { get; private set; }

    /// <summary>Risco antifraude apurado.</summary>
    public FraudRiskLevel FraudRisk { get; private set; }

    /// <summary>Horário limite (cutoff) para liquidação.</summary>
    public DateTime CutoffTime { get; private set; }

    /// <summary>Prioridade calculada (quanto maior, mais cedo a transação deve ser processada).</summary>
    public int Priority { get; private set; }

    /// <summary>Estado atual na fila.</summary>
    public TransactionStatus Status { get; private set; }

    /// <summary>Campo auxiliar para a exclusão lógica (false = excluída).</summary>
    public bool Active { get; private set; }

    /// <summary>Data/hora de entrada na fila. Também é o critério de desempate da prioridade.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Data/hora da última alteração.</summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>Data/hora da exclusão lógica, se aplicável.</summary>
    public DateTime? DeletedAt { get; private set; }

    // Construtor sem parâmetros exigido pelo EF Core (materialização).
    private Transaction()
    {
        Cpf = string.Empty;
        Description = string.Empty;
    }

    private Transaction(
        Guid id, string cpf, string description, string? reference, decimal amount,
        TransactionType type, ClientType clientType, FraudRiskLevel fraudRisk,
        DateTime cutoffTime, DateTime createdAt)
    {
        Id = id;
        Cpf = cpf;
        Description = description;
        Reference = reference;
        Amount = amount;
        Type = type;
        ClientType = clientType;
        FraudRisk = fraudRisk;
        CutoffTime = cutoffTime;
        Status = TransactionStatus.Waiting;
        Active = true;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Fábrica da entidade. Valida as invariantes de domínio (defesa em profundidade,
    /// além da validação de formato feita na camada de API).
    /// </summary>
    public static Transaction Create(
        string cpf, string description, string? reference, decimal amount,
        TransactionType type, ClientType clientType, FraudRiskLevel fraudRisk,
        DateTime cutoffTime, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            throw new BusinessRuleException("O CPF/CNPJ do cliente é obrigatório.");
        if (string.IsNullOrWhiteSpace(description))
            throw new BusinessRuleException("A descrição da transação é obrigatória.");
        if (amount <= 0)
            throw new BusinessRuleException("O valor da transação deve ser maior que zero.");

        return new Transaction(
            Guid.NewGuid(), cpf.Trim(), description.Trim(), reference?.Trim(),
            amount, type, clientType, fraudRisk, cutoffTime, now);
    }

    /// <summary>Atribui a prioridade calculada pela regra de negócio.</summary>
    public void AssignPriority(int priority)
    {
        if (priority < 0)
            throw new BusinessRuleException("A prioridade não pode ser negativa.");
        Priority = priority;
    }

    /// <summary>
    /// Exclusão lógica: não remove o registro, apenas altera o status para
    /// <see cref="TransactionStatus.Deleted"/> e marca os campos de auditoria.
    /// </summary>
    public void SoftDelete(DateTime now)
    {
        if (Status == TransactionStatus.Deleted)
            throw new BusinessRuleException("A transação já foi excluída anteriormente.");

        Status = TransactionStatus.Deleted;
        Active = false;
        DeletedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Atualiza os campos editáveis da transação. O CPF não é alterável.
    /// A prioridade deve ser recalculada externamente após este método (via <see cref="AssignPriority"/>).
    /// </summary>
    public void Update(
        string description, string? reference, decimal amount,
        TransactionType type, ClientType clientType, FraudRiskLevel fraudRisk,
        DateTime cutoffTime, DateTime now)
    {
        if (Status == TransactionStatus.Deleted)
            throw new BusinessRuleException("Não é possível atualizar uma transação excluída.");
        if (string.IsNullOrWhiteSpace(description))
            throw new BusinessRuleException("A descrição da transação é obrigatória.");
        if (amount <= 0)
            throw new BusinessRuleException("O valor da transação deve ser maior que zero.");

        Description = description.Trim();
        Reference = reference?.Trim();
        Amount = amount;
        Type = type;
        ClientType = clientType;
        FraudRisk = fraudRisk;
        CutoffTime = cutoffTime;
        UpdatedAt = now;
    }

    /// <summary>
    /// Altera o status da transação. Não pode ser usado para marcar como <see cref="TransactionStatus.Deleted"/>
    /// (use <see cref="SoftDelete"/> para isso).
    /// </summary>
    public void UpdateStatus(TransactionStatus newStatus, DateTime now)
    {
        if (Status == TransactionStatus.Deleted)
            throw new BusinessRuleException("Não é possível alterar o status de uma transação excluída.");
        if (newStatus == TransactionStatus.Deleted)
            throw new BusinessRuleException("Para excluir uma transação, utilize o endpoint DELETE.");

        Status = newStatus;
        UpdatedAt = now;
    }
}
