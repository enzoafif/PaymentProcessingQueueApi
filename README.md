# PaymentProcessingQueueApi — Fila de Prioridade para Processamento de Pagamentos

API REST que modela uma **fila de prioridade de transações financeiras** (PIX, TED, boletos e
remessas internacionais) apoiada em um **Heap binário**. Trabalho da disciplina *Códigos de Alta
Performance* — cenário **12.10 – Processamento de Pagamentos** (`PaymentProcessingQueueApi`,
recurso `Transação`).

> **Por que uma fila de prioridade, e não FIFO?** No processamento de pagamentos, a próxima
> transação a ser executada não é necessariamente a que chegou primeiro: transações de **alto
> valor** ou com **horário limite (cutoff) próximo** precisam ser liquidadas antes para evitar
> **multas contratuais** e garantir **liquidez** ao sistema financeiro. Uma fila FIFO comum
> ignoraria essa urgência de negócio.

---

## ⚠️ Escopo desta entrega

Esta é uma **entrega parcial**. Os itens abaixo previstos no enunciado **ainda serão
implementados** numa próxima etapa (roadmap):

| Item do enunciado | Situação | Próxima etapa |
|-------------------|----------|---------------|
| Docker Compose | 🚧 A implementar | Subir o banco (e a API) via `docker-compose.yml` |
| Banco de dados real | 🚧 A implementar | Hoje roda em **EF Core InMemory**; migrar para um banco relacional (ex.: SQL Server/PostgreSQL) — a troca é isolada em `Infrastructure/DependencyInjection.cs` (ver abaixo) |
| Testes unitários | 🚧 A implementar | Cobrir a regra de prioridade, o desempate e a exclusão lógica |
| Endpoints restantes | 🚧 A implementar | Hoje há **POST**, **GET /{id}** e **DELETE**; faltam listagem paginada, busca e **PUT** |

Já está implementado e funcional: regra de prioridade explícita e derivada, tratamento de
empate, exclusão lógica, Heap, arquitetura em camadas (Clean Architecture/DDD), SOLID,
Swagger/OpenAPI e validação/tratamento de erros.

> **Observação sobre a rota:** como o código foi escrito em inglês, o recurso é exposto em
> `/transactions` (o enunciado sugere `/transacoes`). É só uma string em `TransactionsController`
> caso queira renomear.

---

## 🚀 Como executar

Pré-requisito: **.NET SDK 9**.

```bash
cd PaymentProcessingQueueApi
dotnet run --project src/PaymentProcessingQueueApi.Api
```

A aplicação sobe em `http://localhost:5272`. A raiz (`/`) redireciona para o **Swagger**:

```
http://localhost:5272/swagger
```

Na inicialização, uma **massa inicial (seed)** com 4 transações de prioridades diferentes é
carregada automaticamente, facilitando a demonstração.

---

## 🏛️ Arquitetura

Solução .NET com 4 projetos, inspirada em **Clean Architecture + DDD**. As setas indicam a
direção das dependências (o Domínio não depende de ninguém):

```
Api  ──►  Application  ──►  Domain  ◄──  Infrastructure
 └──────────────────────────────────────────┘
            (Api também referencia Infrastructure apenas para compor a DI)
```

```
PaymentProcessingQueueApi/
├── src/
│   ├── PaymentProcessingQueueApi.Api/            → Apresentação (HTTP)
│   │   ├── Controllers/        TransactionsController
│   │   ├── Requests/           CreateTransactionRequest (validação de formato)
│   │   ├── Responses/          TransactionResponse
│   │   ├── Mappings/           DTO → Response
│   │   ├── Middlewares/        ExceptionHandlingMiddleware (erros → ProblemDetails)
│   │   └── Program.cs          composição, Swagger, seed
│   │
│   ├── PaymentProcessingQueueApi.Application/     → Casos de uso (orquestração)
│   │   ├── UseCases/           CreateTransaction / GetTransactionById / DeleteTransaction
│   │   ├── Interfaces/         ITransactionRepository (abstração de persistência)
│   │   ├── DTOs/               TransactionDto + mapeamento
│   │   └── Exceptions/         ResourceNotFoundException
│   │
│   ├── PaymentProcessingQueueApi.Domain/          → Coração do software (sem dependências)
│   │   ├── Entities/           Transaction (entidade rica)
│   │   ├── Enums/              TransactionStatus / Type / ClientType / FraudRiskLevel
│   │   ├── PriorityRules/      IPriorityRule + TransactionPriorityRule (a regra!)
│   │   ├── DataStructures/     BinaryHeap<T>  (o Heap!)
│   │   ├── Services/           TransactionPriorityQueue + TransactionPriorityComparer
│   │   ├── Abstractions/       IClock
│   │   └── Exceptions/         BusinessRuleException
│   │
│   └── PaymentProcessingQueueApi.Infrastructure/  → Detalhes técnicos
│       ├── Persistence/        AppDbContext + Configurations + TransactionSeeder
│       ├── Repositories/       TransactionRepository (EF Core)
│       └── Time/               SystemClock
│
└── README.md
```

### Responsabilidade de cada camada
- **Api (Apresentação):** recebe a requisição, valida o **formato** (DataAnnotations), mapeia
  para DTO e devolve HTTP. Sem regra de negócio.
- **Application:** coordena o caso de uso — busca via interface de repositório, chama a regra de
  domínio e persiste. Não conhece EF Core.
- **Domain:** entidade, enums, **regra de prioridade** e **estrutura de dados (Heap)**. Não
  depende de nenhum framework.
- **Infrastructure:** implementa o repositório (EF Core), o relógio e o seed.

### Onde aparecem os princípios SOLID
- **S**RP — cada classe tem um motivo de mudança (regra, fila, repositório, mapeadores separados).
- **O**CP — `IPriorityRule` permite novas regras de prioridade sem alterar os consumidores.
- **L**SP — `BinaryHeap<T>` funciona com qualquer `IComparer<T>` respeitando o contrato.
- **I**SP — `ITransactionRepository` expõe só o necessário aos casos de uso.
- **D**IP — Application/Domain dependem de **abstrações** (`ITransactionRepository`, `IClock`,
  `IPriorityRule`); a Infrastructure as implementa.

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

> A prioridade é fixada no **cadastro** (referência de tempo = `CreatedAt`). Um endpoint `PUT`
> (fora do escopo) recalcularia o score quando os dados mudassem.

---

## 🌳 Onde o Heap se encaixa

A **fila de prioridade** é o conceito abstrato; o **Heap binário** é a estrutura de dados concreta
que a implementa de forma eficiente (`BinaryHeap<T>` em `Domain/DataStructures`).

- O elemento de **maior prioridade fica sempre na raiz** → consulta do topo em **O(1)**.
- **Inserção** reposiciona o nó "subindo" (*sift-up*) → **O(log n)**.
- **Remoção do topo** traz o último elemento para a raiz e o "desce" (*sift-down*) → **O(log n)**.

Mapeamento pai/filhos em vetor: filhos de `i` em `2i+1` e `2i+2`; pai em `(i-1)/2`.

`TransactionPriorityQueue` usa o Heap (com o `TransactionPriorityComparer`) para responder
**“qual a próxima transação?”** e **“qual a posição desta transação na fila?”** — este último é
devolvido no campo `positionInQueue` das respostas de POST e GET, tornando o Heap **observável**
pelos endpoints implementados.

Uma fila **FIFO** comum não resolveria o problema: ela atenderia um boleto de R$ 50 cadastrado às
8h antes de uma remessa de R$ 2.000.000 com cutoff em 10 minutos.

---

## 🔌 Endpoints

Base: `http://localhost:5272`

| Método | Rota | Descrição | Sucesso | Erros |
|--------|------|-----------|:-------:|-------|
| `POST` | `/transactions` | Cadastra uma transação (prioridade calculada) | 201 | 400 |
| `GET` | `/transactions/{id}` | Consulta uma transação por id | 200 | 404 |
| `DELETE` | `/transactions/{id}` | Exclusão **lógica** (altera status) | 204 | 404 / 409 |

### Exclusão lógica
O `DELETE` **não apaga** o registro: muda `status` para `Deleted`, `active` para `false` e grava
`deletedAt`. Depois disso, a transação **não aparece** mais no `GET /{id}` (retorna 404) nem
participa da fila de prioridade.

### Exemplo — POST `/transactions`
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

Resposta `201 Created` (resumida):
```json
{
  "id": "0141f31f-ed1d-4f45-a46b-9dc54c3d86de",
  "amount": 120000.00,
  "type": "Pix",
  "priority": 51,
  "status": "Waiting",
  "positionInQueue": 4,
  "priorityComponents": [
    { "factor": "Amount",          "points": 30, "reason": "Valor da transação (≥ R$ 100.000)." },
    { "factor": "CutoffTime",      "points": 3,  "reason": "Proximidade do horário limite (> 4 h do cutoff)." },
    { "factor": "ClientType",      "points": 10, "reason": "Segmento do cliente (Premium)." },
    { "factor": "TransactionType", "points": 8,  "reason": "Tipo de transação (Pix)." },
    { "factor": "FraudRisk",       "points": 0,  "reason": "Ajuste por risco antifraude (Low)." }
  ]
}
```

O campo `priorityComponents` deixa **transparente** como o score foi formado.

### Valores aceitos (enums, como texto)
- `type`: `Pix`, `Ted`, `Boleto`, `InternationalRemittance`
- `clientType`: `Standard`, `Premium`, `Corporate`
- `fraudRisk`: `Low`, `Medium`, `High`

> **CPF/CNPJ:** validado com **dígitos verificadores** (não só o formato). Apenas números, 11
> dígitos (CPF) ou 14 (CNPJ). CPFs de exemplo válidos: `11144477735`, `47207183887`.

### Exemplos com cURL
```bash
# Criar
curl -X POST http://localhost:5272/transactions -H "Content-Type: application/json" \
  -d '{"cpf":"47207183887","description":"TED urgente","amount":500000,"type":"Ted","clientType":"Corporate","fraudRisk":"Low","cutoffTime":"2026-06-11T13:00:00"}'

# Consultar (troque o {id})
curl http://localhost:5272/transactions/{id}

# Excluir logicamente
curl -X DELETE http://localhost:5272/transactions/{id}
```

---

## 🗄️ Banco de dados (InMemory) e como migrar para um banco real

A persistência usa **EF Core InMemory** com o padrão **Repository** e `DbContext`. Para trocar por
um banco relacional, basta **uma linha** em `Infrastructure/DependencyInjection.cs`:

```csharp
// De:
services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("PaymentProcessingQueueDb"));
// Para (ex.: SQL Server):
services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));
```

O mapeamento (`TransactionConfiguration`) já grava os enums como texto e define tamanhos/precisão,
prontos para um schema relacional.

---

## 🧰 Stack
- .NET 9 / ASP.NET Core (Controllers)
- EF Core 9 (provedor InMemory)
- Swashbuckle (Swagger/OpenAPI)
