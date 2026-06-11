using Microsoft.AspNetCore.Mvc;
using PaymentProcessingQueueApi.Api.Mappings;
using PaymentProcessingQueueApi.Api.Requests;
using PaymentProcessingQueueApi.Api.Responses;
using PaymentProcessingQueueApi.Application.UseCases.CreateTransaction;
using PaymentProcessingQueueApi.Application.UseCases.DeleteTransaction;
using PaymentProcessingQueueApi.Application.UseCases.GetTransactionById;

namespace PaymentProcessingQueueApi.Api.Controllers;

/// <summary>
/// Recurso "transações" da fila de processamento de pagamentos (cenário 12.10).
/// A camada de apresentação apenas recebe a requisição, valida o formato, delega ao
/// caso de uso e devolve a resposta HTTP — sem regra de negócio.
/// </summary>
[ApiController]
[Route("transactions")]
[Produces("application/json")]
public sealed class TransactionsController : ControllerBase
{
    private readonly CreateTransactionUseCase _createTransaction;
    private readonly GetTransactionByIdUseCase _getTransactionById;
    private readonly DeleteTransactionUseCase _deleteTransaction;

    public TransactionsController(
        CreateTransactionUseCase createTransaction,
        GetTransactionByIdUseCase getTransactionById,
        DeleteTransactionUseCase deleteTransaction)
    {
        _createTransaction = createTransaction;
        _getTransactionById = getTransactionById;
        _deleteTransaction = deleteTransaction;
    }

    /// <summary>Cadastra uma transação na fila de processamento.</summary>
    /// <remarks>
    /// A prioridade é calculada automaticamente a partir dos dados informados
    /// (valor, horário limite, tipo de cliente, tipo de transação e risco antifraude).
    /// </remarks>
    /// <response code="201">Transação cadastrada com a prioridade calculada.</response>
    /// <response code="400">Dados de entrada inválidos.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionResponse>> Create(
        [FromBody] CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        // As anotações [Required]/[EnumDataType] garantem que os valores não são nulos aqui.
        var command = new CreateTransactionCommand(
            request.Cpf,
            request.Description,
            request.Reference,
            request.Amount,
            request.Type!.Value,
            request.ClientType!.Value,
            request.FraudRisk!.Value,
            request.CutoffTime!.Value);

        var dto = await _createTransaction.ExecuteAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto.ToResponse());
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
}
