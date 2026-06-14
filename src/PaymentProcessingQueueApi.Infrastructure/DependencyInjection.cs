using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não configurada.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name)));

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
