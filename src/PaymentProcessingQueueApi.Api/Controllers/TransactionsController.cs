using Microsoft.AspNetCore.Mvc;
using PaymentProcessingQueueApi.Api.Mappings;
using PaymentProcessingQueueApi.Api.Requests;
using PaymentProcessingQueueApi.Api.Responses;
using PaymentProcessingQueueApi.Application.UseCases.AttendNextTransaction;
using PaymentProcessingQueueApi.Application.UseCases.CreateTransaction;
using PaymentProcessingQueueApi.Application.UseCases.DeleteTransaction;
using PaymentProcessingQueueApi.Application.UseCases.GetNextTransaction;
using PaymentProcessingQueueApi.Application.UseCases.GetPagedTransactions;
using PaymentProcessingQueueApi.Application.UseCases.GetStatistics;
using PaymentProcessingQueueApi.Application.UseCases.GetTransactionById;
using PaymentProcessingQueueApi.Application.UseCases.SearchTransactions;
using PaymentProcessingQueueApi.Application.UseCases.UpdateTransaction;
using PaymentProcessingQueueApi.Application.UseCases.UpdateTransactionStatus;

namespace PaymentProcessingQueueApi.Api.Controllers;

/// <summary>
/// Recurso "transações" da fila de processamento de pagamentos (cenário 12.10).
/// A camada de apresentação apenas recebe a requisição, valida o formato, delega ao
/// caso de uso e devolve a resposta HTTP — sem regra de negócio.
/// </summary>
[ApiController]
[Route("transacoes")]
[Produces("application/json")]
public sealed class TransactionsController : ControllerBase
{
    private readonly CreateTransactionUseCase _createTransaction;
    private readonly GetTransactionByIdUseCase _getTransactionById;
    private readonly DeleteTransactionUseCase _deleteTransaction;
    private readonly GetPagedTransactionsUseCase _getPagedTransactions;
    private readonly SearchTransactionsUseCase _searchTransactions;
    private readonly UpdateTransactionUseCase _updateTransaction;
    private readonly GetNextTransactionUseCase _getNextTransaction;
    private readonly AttendNextTransactionUseCase _attendNextTransaction;
    private readonly UpdateTransactionStatusUseCase _updateTransactionStatus;
    private readonly GetStatisticsUseCase _getStatistics;

    public TransactionsController(
        CreateTransactionUseCase createTransaction,
        GetTransactionByIdUseCase getTransactionById,
        DeleteTransactionUseCase deleteTransaction,
        GetPagedTransactionsUseCase getPagedTransactions,
        SearchTransactionsUseCase searchTransactions,
        UpdateTransactionUseCase updateTransaction,
        GetNextTransactionUseCase getNextTransaction,
        AttendNextTransactionUseCase attendNextTransaction,
        UpdateTransactionStatusUseCase updateTransactionStatus,
        GetStatisticsUseCase getStatistics)
    {
        _createTransaction = createTransaction;
        _getTransactionById = getTransactionById;
        _deleteTransaction = deleteTransaction;
        _getPagedTransactions = getPagedTransactions;
        _searchTransactions = searchTransactions;
        _updateTransaction = updateTransaction;
        _getNextTransaction = getNextTransaction;
        _attendNextTransaction = attendNextTransaction;
        _updateTransactionStatus = updateTransactionStatus;
        _getStatistics = getStatistics;
    }

    // ─── Criação ────────────────────────────────────────────────────────────────

    /// <summary>Cadastra uma transação na fila de processamento.</summary>
    /// <remarks>A prioridade é calculada automaticamente a partir dos dados informados.</remarks>
    /// <response code="201">Transação cadastrada com a prioridade calculada.</response>
    /// <response code="400">Dados de entrada inválidos.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionResponse>> Create(
        [FromBody] CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateTransactionCommand(
            request.Cpf, request.Description, request.Reference, request.Amount,
            request.Type!.Value, request.ClientType!.Value, request.FraudRisk!.Value,
            request.CutoffTime!.Value);

        var dto = await _createTransaction.ExecuteAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto.ToResponse());
    }

    // ─── Listagem e busca ────────────────────────────────────────────────────────

    /// <summary>Lista as transações ativas paginadas, ordenadas por prioridade decrescente.</summary>
    /// <param name="page">Página (base 1). Padrão: 1.</param>
    /// <param name="size">Itens por página. Padrão: 10.</param>
    /// <response code="200">Lista paginada de transações.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedTransactionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedTransactionResponse>> GetPaged(
        [FromQuery] int page = 1, [FromQuery] int size = 10, CancellationToken cancellationToken = default)
    {
        var result = await _getPagedTransactions.ExecuteAsync(page, size, cancellationToken);
        return Ok(result.ToPagedResponse());
    }

    /// <summary>Busca transações ativas por descrição (contains, case-insensitive).</summary>
    /// <param name="descricao">Termo a ser buscado na descrição.</param>
    /// <response code="200">Lista de transações que correspondem à busca.</response>
    /// <response code="400">Parâmetro de busca não informado.</response>
    [HttpGet("buscar")]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> Search(
        [FromQuery] string descricao, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            return BadRequest("O parâmetro 'descricao' é obrigatório.");

        var dtos = await _searchTransactions.ExecuteAsync(descricao, cancellationToken);
        return Ok(dtos.Select(d => d.ToResponse()).ToList());
    }

    /// <summary>Consulta uma transação específica pelo identificador.</summary>
    /// <response code="200">Transação encontrada.</response>
    /// <response code="404">Transação inexistente ou excluída logicamente.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _getTransactionById.ExecuteAsync(id, cancellationToken);
        return Ok(dto.ToResponse());
    }

    // ─── Fila de prioridade ──────────────────────────────────────────────────────

    /// <summary>Retorna a próxima transação da fila (maior prioridade, status Waiting) sem alterar seu estado.</summary>
    /// <response code="200">Próxima transação a ser processada.</response>
    /// <response code="204">Fila vazia — nenhuma transação aguardando.</response>
    [HttpGet("proximo")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<TransactionResponse>> GetNext(CancellationToken cancellationToken)
    {
        var dto = await _getNextTransaction.ExecuteAsync(cancellationToken);
        return dto is null ? NoContent() : Ok(dto.ToResponse());
    }

    /// <summary>Seleciona a próxima transação da fila e muda seu status para Processing.</summary>
    /// <response code="200">Transação selecionada e em processamento.</response>
    /// <response code="404">Nenhuma transação aguardando na fila.</response>
    [HttpPost("proximo/atender")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> AttendNext(CancellationToken cancellationToken)
    {
        var dto = await _attendNextTransaction.ExecuteAsync(cancellationToken);
        return Ok(dto.ToResponse());
    }

    // ─── Atualização ─────────────────────────────────────────────────────────────

    /// <summary>Atualiza os dados de uma transação e recalcula a prioridade automaticamente.</summary>
    /// <response code="200">Transação atualizada com nova prioridade.</response>
    /// <response code="400">Dados de entrada inválidos.</response>
    /// <response code="404">Transação inexistente ou excluída.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> Update(
        Guid id, [FromBody] UpdateTransactionRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateTransactionCommand(
            id, request.Description, request.Reference, request.Amount,
            request.Type!.Value, request.ClientType!.Value, request.FraudRisk!.Value,
            request.CutoffTime!.Value);

        var dto = await _updateTransaction.ExecuteAsync(command, cancellationToken);
        return Ok(dto.ToResponse());
    }

    /// <summary>Atualiza apenas o status de uma transação (Waiting, Processing ou Completed).</summary>
    /// <response code="200">Status atualizado.</response>
    /// <response code="400">Status inválido.</response>
    /// <response code="404">Transação inexistente ou excluída.</response>
    /// <response code="409">Operação não permitida pelo domínio.</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TransactionResponse>> UpdateStatus(
        Guid id, [FromBody] UpdateStatusRequest request, CancellationToken cancellationToken)
    {
        var dto = await _updateTransactionStatus.ExecuteAsync(id, request.Status!.Value, cancellationToken);
        return Ok(dto.ToResponse());
    }

    // ─── Exclusão lógica ─────────────────────────────────────────────────────────

    /// <summary>Exclui logicamente uma transação (altera o status; não remove fisicamente).</summary>
    /// <response code="204">Transação excluída logicamente.</response>
    /// <response code="404">Transação inexistente.</response>
    /// <response code="409">Transação já excluída anteriormente.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _deleteTransaction.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }

    // ─── Estatísticas ────────────────────────────────────────────────────────────

    /// <summary>Retorna a contagem de transações agrupada por status.</summary>
    /// <response code="200">Estatísticas por status.</response>
    [HttpGet("estatisticas")]
    [ProducesResponseType(typeof(StatisticsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StatisticsResponse>> GetStatistics(CancellationToken cancellationToken)
    {
        var dto = await _getStatistics.ExecuteAsync(cancellationToken);
        return Ok(dto.ToResponse());
    }
}
