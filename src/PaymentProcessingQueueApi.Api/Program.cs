using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.OpenApi.Models;
using PaymentProcessingQueueApi.Api.Middlewares;
using PaymentProcessingQueueApi.Application;
using PaymentProcessingQueueApi.Domain.Abstractions;
using PaymentProcessingQueueApi.Domain.PriorityRules;
using PaymentProcessingQueueApi.Infrastructure;
using PaymentProcessingQueueApi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums serializados/desserializados como texto (ex.: "Pix" em vez de 0).
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Documentação OpenAPI/Swagger.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PaymentProcessingQueueApi",
        Version = "v1",
        Description = "Fila de prioridade para processamento de pagamentos (transações financeiras), " +
                      "apoiada em um Heap binário. Cenário 12.10 — Códigos de Alta Performance."
    });

    // Inclui os comentários XML (descrições dos endpoints) na documentação.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// Composição das camadas.
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

// Tratamento global de exceções deve envolver todo o pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger sempre habilitado (facilita a avaliação).
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "PaymentProcessingQueueApi v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();

// Redireciona a raiz para o Swagger (oculto da documentação: é só conveniência, não um endpoint da API).
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Massa inicial de dados (seed).
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();

    var priorityRule = services.GetRequiredService<IPriorityRule>();
    var clock = services.GetRequiredService<IClock>();
    await TransactionSeeder.SeedAsync(context, priorityRule, clock);
}

// Em desenvolvimento, abre o Swagger no navegador automaticamente ao iniciar.
// (Funciona inclusive com `dotnet run`, que — ao contrário do `launchBrowser` do
// launchSettings — não abre o navegador sozinho.)
if (app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?
            .Addresses.FirstOrDefault();

        if (address is null) return;

        // Normaliza curingas de bind para um host acessível pelo navegador.
        var url = address.Replace("0.0.0.0", "localhost").Replace("[::]", "localhost");

        try
        {
            Process.Start(new ProcessStartInfo($"{url}/swagger") { UseShellExecute = true });
        }
        catch
        {
            // Se o navegador não puder ser aberto, apenas siga: a URL é exibida no console.
        }
    });
}

app.Run();
