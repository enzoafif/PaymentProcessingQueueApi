using Microsoft.AspNetCore.Mvc;
using PaymentProcessingQueueApi.Application.Exceptions;
using PaymentProcessingQueueApi.Domain.Exceptions;

namespace PaymentProcessingQueueApi.Api.Middlewares;

/// <summary>
/// Converte exceções de domínio/aplicação em respostas HTTP padronizadas (ProblemDetails):
///   • <see cref="ResourceNotFoundException"/> → 404
///   • <see cref="BusinessRuleException"/>      → 409
///   • demais exceções                          → 500
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ResourceNotFoundException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Recurso não encontrado", ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, "Operação não permitida", ex.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Cliente cancelou/desconectou: não é erro do servidor — não loga como erro nem responde.
            _logger.LogInformation("Requisição cancelada pelo cliente: {Method} {Path}.",
                context.Request.Method, context.Request.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado ao processar {Method} {Path}.",
                context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "Erro interno", "Ocorreu um erro inesperado.");
        }
    }

    private async Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
    {
        // Se a resposta já começou a ser enviada, não dá para reescrever status/headers/corpo.
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Resposta já iniciada; não foi possível escrever o ProblemDetails ({Status}).", status);
            return;
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        // Uma falha ao serializar/escrever (ex.: cliente desconectado) não deve derrubar o pipeline.
        try
        {
            await context.Response.WriteAsJsonAsync(problem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao escrever o ProblemDetails na resposta.");
        }
    }
}
