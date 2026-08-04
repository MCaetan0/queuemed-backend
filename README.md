# QueueMed Backend

API ASP.NET Core 8 para gestão de fila de espera clínica via QR code.

## Stack

- .NET 8 / ASP.NET Core Web API
- Redis (estado temporário da fila + backplane SignalR)
- SignalR em tempo real
- JWT para atendentes (credenciais via config/Seed)
- FluentValidation / QRCoder

## Configuração

1. Copie o template:

```bash
cp .env.example .env
```

2. Suba o Redis local:

```bash
docker compose up -d
```

3. No `.env`, defina as variáveis Redis (a API monta a connection string):

| Variável | Exemplo |
|----------|---------|
| `Redis__Host` | `localhost` ou `host.proxy.rlwy.net` |
| `Redis__Port` | `6379` ou `48041` (Railway) |
| `Redis__Password` | senha do Redis |
| `Redis__User` | `default` (Railway) |
| `Redis__AbortConnect` | `false` |

Opcional: `Jwt__Key`, `Seed__AtendenteSenha`.

A API carrega o `.env` no startup (DotNetEnv). **Redis é obrigatório** — sem ele a API não sobe.

## Executar a API

```bash
dotnet run --project src/QueueMed.Api
```

Swagger: `http://localhost:5172/swagger` (porta conforme `launchSettings`).

### Credenciais seed

- Usuário: `atendente`
- Senha: `Atendente@123` (ou o valor de `Seed__AtendenteSenha` no `.env`)

## Endpoints

| Método | Rota | Auth |
|--------|------|------|
| POST | `/tickets` | público |
| GET | `/tickets/{id}` | público |
| DELETE | `/tickets/{id}` | JWT |
| POST | `/auth/login` | público |
| GET | `/fila?especialidade=Clinico\|Psiquiatra` | JWT |
| POST | `/fila/chamar-proximo` | JWT |
| GET | `/admin/qrcode` | JWT |

Enums no JSON usam string: `Preferencial`, `Normal`, `Clinico`, `Psiquiatra`, `Aguardando`, `Chamado`.

Fluxo: entrar na fila (`Aguardando`) → chamar próximo (`Chamado`) → remover (`DELETE`).

## Tempo real (SignalR)

Hub: `/hubs/fila`.

Métodos do cliente → servidor:

- `JoinTicket(ticketId)` / `LeaveTicket(ticketId)`
- `JoinFila(especialidade)` / `LeaveFila(especialidade)`

Eventos servidor → cliente:

- `TicketUpdated` — status/posição do ticket
- `FilaUpdated` — lista ordenada da especialidade
- `TicketChamado` — próximo chamado

Para painel autenticado via WebSocket, envie `?access_token=<jwt>` na URL do hub.

## Regra de prioridade

Após **N** chamadas preferenciais consecutivas (por especialidade), intercala **1** normal se houver.  
Config: `Queue:PreferentialCallsBeforeNormal` (default `2`).

Dados da fila expiram automaticamente após `Queue:DataTtlHours` (default `24`).

## Testes

```bash
dotnet test
```

## Estrutura

```
src/QueueMed.Domain
src/QueueMed.Application
src/QueueMed.Infrastructure
src/QueueMed.Api
tests/QueueMed.Application.Tests
```
