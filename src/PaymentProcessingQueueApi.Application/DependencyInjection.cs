using Microsoft.Extensions.DependencyInjection;
using PaymentProcessingQueueApi.Application.UseCases.CreateTransaction;
using PaymentProcessingQueueApi.Application.UseCases.DeleteTransaction;
using PaymentProcessingQueueApi.Application.UseCases.GetTransactionById;
using PaymentProcessingQueueApi.Domain.PriorityRules;
using PaymentProcessingQueueApi.Domain.Services;

namespace PaymentProcessingQueueApi.Application;

/// <summary>Registro de dependências da camada de Aplicação (e das regras de domínio).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Regra de prioridade e fila (domínio) — sem estado, podem ser singletons.
        services.AddSingleton<IPriorityRule, TransactionPriorityRule>();
        services.AddSingleton<TransactionPriorityQueue>();

        // Casos de uso.
        services.AddScoped<CreateTransactionUseCase>();
        services.AddScoped<GetTransactionByIdUseCase>();
        services.AddScoped<DeleteTransactionUseCase>();

        return services;
    }
}
