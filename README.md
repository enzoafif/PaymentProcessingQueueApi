# PaymentProcessingQueueApi — Fila de Prioridade para Processamento de Pagamentos

API REST que modela uma **fila de prioridade de transações financeiras** (PIX, TED, boletos e
remessas internacionais) apoiada em um **Heap binário**. Trabalho da disciplina *Códigos de Alta
Performance* — cenário **12.10 – Processamento de Pagamentos** (`PaymentProcessingQueueApi`,
recurso `Transação`).

> **Por que uma fila de prioridade, e não FIFO?** No processamento de pagamentos, a próxima
> transação a ser executada não é necessariamente a que chegou primeiro: transações de **alto
> valor** ou com **horário limite (cutoff) próximo** precisam ser liquidadas antes para evitar
> **multas contratuais** e garantir **liquidez** ao sistema financeiro.

---

## 🚀 Como executar

### Opção 1 — Docker Compose (recomendado)

Pré-requisito: **Docker Desktop**.

```bash
cd PaymentProcessingQueueApi
docker-compose up --build
```

A API sobe em `http://localhost:5089`. O Swagger estará em:

```
http://localhost:5089/swagger
```

### Opção 2 — Localmente com banco via Docker

Pré-requisito: **.NET SDK 9** e **Docker Desktop**.

```bash
# 1) Sobe apenas o banco
docker-compose up postgres

# 2) Em outro terminal, executa a API
dotnet run --project src/PaymentProcessingQueueApi.Api
```

A aplicação sobe em `http://localhost:5272`. As migrations são aplicadas automaticamente na
inicialização (`context.Database.Migrate()`).

Na inicialização, uma **massa inicial (seed)** com 4 transações de prioridades diferentes é
carregada automaticamente.

---

## 🏛️ Arquitetura

Solução .NET com 5 projetos, inspirada em **Clean Architecture + DDD**. As setas indicam a
direção das dependências (o Domínio não depende de ninguém):

```
Api  ──►  Application  ──►  Domain  ◄──  Infrastructure
 └──────────────────────────────────────────┘
            (Api também referencia Infrastructure apenas para compor a DI)
```

```
PaymentProcessingQueueApi/
├── docker-compose.yaml
├── Dockerfile
├── src/
│   ├── PaymentProcessingQueueApi.Api/            → Apresentação (HTTP)
│   │   ├── Controllers/        TransactionsController
│   │   ├── Requests/           CreateTransactionRequest, UpdateTransactionRequest, UpdateStatusRequest
│   │   ├── Responses/          TransactionResponse, PagedTransactionResponse, StatisticsResponse
│   │   ├── Mappings/           DTO → Response
│   │   ├── Middlewares/        ExceptionHandlingMiddleware (erros → ProblemDetails)
│   │   └── Program.cs          composição, Swagger, seed, migrate
│   │
│   ├── PaymentProcessingQueueApi.Application/     → Casos de uso (orquestração)
│   │   ├── UseCases/           10 casos de uso
│   │   ├── Interfaces/         ITransactionRepository (abstração de persistência)
│   │   ├── DTOs/               TransactionDto, PagedResultDto, TransactionStatisticsDto
│   │   └── Exceptions/         ResourceNotFoundException
│   │
│   ├── PaymentProcessingQueueApi.Domain/          → Coração do software (sem dependências)
│   │   ├── Entities/           Transaction (entidade rica: Create, Update, UpdateStatus, SoftDelete)
│   │   ├── Enums/              TransactionStatus / Type / ClientType / FraudRiskLevel
│   │   ├── PriorityRules/      IPriorityRule + TransactionPriorityRule
│   │   ├── DataStructures/     BinaryHeap<T>
│   │   ├── Services/           TransactionPriorityQueue + TransactionPriorityComparer
│   │   ├── Abstractions/       IClock
│   │   └── Exceptions/         BusinessRuleException
│   │
│   └── PaymentProcessingQueueApi.Infrastructure/  → Detalhes técnicos
│       ├── Migrations/         InitialCreate (PostgreSQL)
│       ├── Persistence/        AppDbContext + Configurations + TransactionSeeder
│       ├── Repositories/       TransactionRepository (EF Core + Npgsql)
│       └── Time/               SystemClock
│
└── tests/
    └── PaymentProcessingQueueApi.UnitTests/       → 33 testes unitários (xUnit)
        ├── PriorityRules/      TransactionPriorityRuleTests
        ├── DataStructures/     BinaryHeapTests
        └── Domain/             TransactionSoftDeleteTests
```

---

## 🧮 Regra de prioridade (explícita e derivada)

A prioridade **não é digitada pelo usuário**: o cliente cadastra os dados da transação e o sistema
calcula um **score** por uma tabela de decisão (`TransactionPriorityRule`). **Quanto maior o
score, mais cedo a transação é processada.** Faixa: 0 a 100 (antes de penalidades de risco).

| Fator | Critério | Pontos |
|-------|----------|-------:|
| **Valor** | ≥ R$ 1.000.000 / ≥ 100.000 / ≥ 10.000 / ≥ 1.000 / < 1.000 | 40 / 30 / 20 / 10 / 5 |
| **Horário limite (cutoff)** | vencido / ≤15min / ≤1h / ≤4h / >4h | 35 / 30 / 20 / 10 / 3 |
| **Tipo de cliente** | Corporate / Premium / Standard | 15 / 10 / 3 |
| **Tipo de transação** | Remessa internacional / PIX / TED / Boleto | 10 / 8 / 5 / 3 |
| **Risco antifraude** | High / Medium / Low | −20 / −5 / 0 |

O **risco antifraude penaliza** o score: uma transação suspeita de alto valor **não deve "furar a
fila"** sem revisão. O total nunca fica negativo (piso = 0).

### Tratamento de empate
Se duas transações têm o **mesmo score**, vence a **mais antiga** (`CreatedAt` menor) — quem
espera há mais tempo é atendido primeiro. Implementado em `TransactionPriorityComparer`.

---

## 🌳 Onde o Heap se encaixa

A **fila de prioridade** é o conceito abstrato; o **Heap binário** é a estrutura de dados concreta
que a implementa de forma eficiente (`BinaryHeap<T>` em `Domain/DataStructures`).

- O elemento de **maior prioridade fica sempre na raiz** → consulta do topo em **O(1)**.
- **Inserção** reposiciona o nó "subindo" (*sift-up*) → **O(log n)**.
- **Remoção do topo** traz o último elemento para a raiz e o "desce" (*sift-down*) → **O(log n)**.

---

## 🔌 Endpoints

Base: `http://localhost:5272` (local) | `http://localhost:5089` (Docker)

| Método | Rota | Descrição | Sucesso | Erros |
|--------|------|-----------|:-------:|-------|
| `POST` | `/transacoes` | Cadastra uma transação (prioridade calculada automaticamente) | 201 | 400 |
| `GET` | `/transacoes?page=1&size=10` | Lista transações ativas paginadas por prioridade | 200 | — |
| `GET` | `/transacoes/buscar?descricao=pix` | Busca por descrição (contains, case-insensitive) | 200 | 400 |
| `GET` | `/transacoes/{id}` | Consulta uma transação por id | 200 | 404 |
| `GET` | `/transacoes/proximo` | Próxima transação da fila (maior prioridade, Waiting) | 200/204 | — |
| `POST` | `/transacoes/proximo/atender` | Seleciona a próxima e muda status para Processing | 200 | 404 |
| `PUT` | `/transacoes/{id}` | Atualiza dados e recalcula prioridade automaticamente | 200 | 400/404 |
| `PATCH` | `/transacoes/{id}/status` | Atualiza apenas o status (Waiting/Processing/Completed) | 200 | 400/404/409 |
| `DELETE` | `/transacoes/{id}` | Exclusão **lógica** (altera status, não remove fisicamente) | 204 | 404/409 |
| `GET` | `/transacoes/estatisticas` | Contagem de transações agrupada por status | 200 | — |

### Exclusão lógica
O `DELETE` **não apaga** o registro: muda `status` para `Deleted`, `active` para `false` e grava
`deletedAt`. Depois disso, a transação **não aparece** em nenhum `GET` comum.

---

## 📋 Exemplos de payload JSON

### POST `/transacoes` — Criar transação
```json
{
  "cpf": "11144477735",
  "description": "PIX agendado folha de pagamento",
  "reference": "PIX-2026-001",
  "amount": 120000.00,
  "type": "Pix",
  "clientType": "Premium",
  "fraudRisk": "Low",
  "cutoffTime": "2026-06-11T18:00:00"
}
```

Resposta `201 Created`:
```json
{
  "id": "0141f31f-ed1d-4f45-a46b-9dc54c3d86de",
  "cpf": "11144477735",
  "description": "PIX agendado folha de pagamento",
  "amount": 120000.00,
  "type": "Pix",
  "clientType": "Premium",
  "fraudRisk": "Low",
  "priority": 51,
  "status": "Waiting",
  "positionInQueue": 3,
  "priorityComponents": [
    { "factor": "Amount",          "points": 30, "reason": "Valor da transação (≥ R$ 100.000)." },
    { "factor": "CutoffTime",      "points": 3,  "reason": "Proximidade do horário limite (> 4 h do cutoff)." },
    { "factor": "ClientType",      "points": 10, "reason": "Segmento do cliente (Premium)." },
    { "factor": "TransactionType", "points": 8,  "reason": "Tipo de transação (Pix)." },
    { "factor": "FraudRisk",       "points": 0,  "reason": "Ajuste por risco antifraude (Low)." }
  ],
  "createdAt": "2026-06-14T10:00:00"
}
```

### GET `/transacoes?page=1&size=10` — Lista paginada
```json
{
  "items": [ /* lista de TransactionResponse */ ],
  "totalItems": 4,
  "totalPages": 1,
  "currentPage": 1,
  "pageSize": 10
}
```

### PUT `/transacoes/{id}` — Atualizar transação
```json
{
  "description": "PIX urgente folha de pagamento - revisado",
  "reference": "PIX-2026-001-REV",
  "amount": 200000.00,
  "type": "Pix",
  "clientType": "Corporate",
  "fraudRisk": "Low",
  "cutoffTime": "2026-06-11T16:00:00"
}
```

### PATCH `/transacoes/{id}/status` — Atualizar apenas o status
```json
{ "status": "Completed" }
```

### GET `/transacoes/estatisticas` — Estatísticas
```json
{
  "waiting":    2,
  "processing": 1,
  "completed":  1,
  "deleted":    0,
  "total":      4
}
```

### Valores aceitos (enums, como texto)
- `type`: `Pix`, `Ted`, `Boleto`, `InternationalRemittance`
- `clientType`: `Standard`, `Premium`, `Corporate`
- `fraudRisk`: `Low`, `Medium`, `High`
- `status` (PATCH): `Waiting`, `Processing`, `Completed`

> **CPF/CNPJ:** validado com **dígitos verificadores**. Apenas números, 11 dígitos (CPF) ou 14
> (CNPJ). CPFs válidos para teste: `11144477735`, `47207183887`, `52998224725`.

---

## 🗄️ Banco de dados (PostgreSQL)

A persistência usa **PostgreSQL** via **EF Core 9 + Npgsql**. As migrations são aplicadas
automaticamente na inicialização. O banco é configurado via connection string:

| Ambiente | Configuração |
|----------|-------------|
| Docker Compose | Variável de ambiente `ConnectionStrings__DefaultConnection` |
| Local | `appsettings.Development.json` → `ConnectionStrings.DefaultConnection` |

Connection string padrão (local):
```
Host=localhost;Port=5432;Database=priority_queue;Username=postgres;Password=postgres
```

---

## 🧪 Testes unitários

```bash
dotnet test tests/PaymentProcessingQueueApi.UnitTests
```

**33 testes** cobrindo:
- `TransactionPriorityRuleTests` — scoring por fator, penalidade de fraude, total nunca negativo
- `BinaryHeapTests` — insert, peek, extractTop, heap vazio, duplicatas, ordem decrescente
- `TransactionSoftDeleteTests` — exclusão lógica, duplo delete, update em entidade excluída, UpdateStatus

---

## 🧰 Stack
- .NET 9 / ASP.NET Core (Controllers)
- EF Core 9 + Npgsql (PostgreSQL)
- Docker / Docker Compose
- Swashbuckle (Swagger/OpenAPI)
- xUnit (testes unitários)
