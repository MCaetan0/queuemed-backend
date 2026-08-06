# QueueMed Backend

API ASP.NET Core 8 para gestão de fila de espera clínica via QR code.

## Stack

- .NET 8 / ASP.NET Core Web API
- Redis (estado temporário da fila + backplane SignalR)
- SignalR em tempo real
- JWT para atendentes (credenciais via config/Seed)
- FluentValidation / QRCoder

## Configuração

1. Copie o template e preencha **todas** as variáveis obrigatórias (sem valores default no código):

```bash
cp .env.example .env
```

| Variável | Obrigatória | Uso |
|----------|-------------|-----|
| `Base__Url` | sim | URL pública (CORS + QR `{Base__Url}/entrar`) |
| `ASPNETCORE_URLS` | local | Bind do Kestrel (em Railway use `PORT`) |
| `Redis__Host` | sim | Host Redis |
| `Redis__Port` | sim* | Porta (*se o host não incluir `:porta`) |
| `Redis__Password` | se o Redis exigir | Senha |
| `Redis__User` | se o Redis exigir | Usuário |
| `Redis__AbortConnect` | sim | `true` / `false` |

Opcional: `QrCode__EntryUrl`, `Cors__Origins__0`, `Jwt__Key`, `Seed__AtendenteSenha`.

A API carrega o `.env` no startup (DotNetEnv). Sem `Base__Url` ou Redis a API **não sobe**.

## Executar a API

```bash
dotnet run --project src/QueueMed.Api
```

Swagger: `{ASPNETCORE_URLS}/swagger` (conforme o `.env`).

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
