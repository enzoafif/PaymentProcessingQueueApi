using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentProcessingQueueApi.Application.Interfaces;
using PaymentProcessingQueueApi.Domain.Abstractions;
using PaymentProcessingQueueApi.Infrastructure.Persistence;
using PaymentProcessingQueueApi.Infrastructure.Repositories;
using PaymentProcessingQueueApi.Infrastructure.Time;

namespace PaymentProcessingQueueApi.Infrastructure;

/// <summary>Registro de dependências da camada de Infraestrutura.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Banco em memória (InMemory). Para trocar por SQL Server/PostgreSQL, basta
        // alterar apenas esta linha e adicionar o pacote do provedor correspondente.
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("PaymentProcessingQueueDb"));

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
