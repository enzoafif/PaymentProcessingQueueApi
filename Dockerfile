FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY PaymentProcessingQueueApi.sln .
COPY NuGet.config .
COPY src/PaymentProcessingQueueApi.Domain/PaymentProcessingQueueApi.Domain.csproj src/PaymentProcessingQueueApi.Domain/
COPY src/PaymentProcessingQueueApi.Application/PaymentProcessingQueueApi.Application.csproj src/PaymentProcessingQueueApi.Application/
COPY src/PaymentProcessingQueueApi.Infrastructure/PaymentProcessingQueueApi.Infrastructure.csproj src/PaymentProcessingQueueApi.Infrastructure/
COPY src/PaymentProcessingQueueApi.Api/PaymentProcessingQueueApi.Api.csproj src/PaymentProcessingQueueApi.Api/

RUN dotnet restore src/PaymentProcessingQueueApi.Api/PaymentProcessingQueueApi.Api.csproj

COPY src/ src/
RUN dotnet publish src/PaymentProcessingQueueApi.Api/PaymentProcessingQueueApi.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "PaymentProcessingQueueApi.Api.dll"]
