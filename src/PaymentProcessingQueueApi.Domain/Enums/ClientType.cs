namespace PaymentProcessingQueueApi.Domain.Enums;

/// <summary>Segmento comercial do cliente (define o SLA contratual de liquidação).</summary>
public enum ClientType
{
    /// <summary>Cliente de varejo padrão.</summary>
    Standard = 0,

    /// <summary>Cliente de alto relacionamento.</summary>
    Premium = 1,

    /// <summary>Cliente corporativo / pessoa jurídica de grande porte.</summary>
    Corporate = 2
}
