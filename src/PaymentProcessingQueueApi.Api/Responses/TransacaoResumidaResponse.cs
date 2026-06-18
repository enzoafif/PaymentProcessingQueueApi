namespace PaymentProcessingQueueApi.Api.Responses;

/// <summary>Visão resumida de uma transação para visualização da estrutura do heap em aula.</summary>
public sealed class TransacaoResumidaResponse
{
    public string Descricao { get; init; } = string.Empty;
    public int Prioridade { get; init; }
    public int IndiceNoHeap { get; init; }
    public string PapelNoHeap { get; init; } = string.Empty;
}
