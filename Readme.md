# LLMath-Backend

Backend for an LLM-powered math tutoring and problem-solving platform. It provides:
- User management via ASP.NET Core Identity
- Chat sessions with different tutoring modes (Tutor, Learning, Guided, Exam)
- LLM integration (configured to work with Ollama/OpenAI-compatible API)
- Integration with GeoLin problem generator and LLMath-Problems storage
- Tasks management and linkage between problems and chats
- Admin/user statistics

The API serves Swagger UI in Development and is designed to be used by the LLMath frontend.

## Architecture
- **Solution layout**
  - `src/MathLLMBackend.Presentation` (ASP.NET Core Web API, DI setup, middleware, controllers)
  - `src/MathLLMBackend.Core` (domain services, prompts/LLM configuration, business logic)
  - `src/MathLLMBackend.DataAccess` (EF Core `AppDbContext`, migrations, warmup/migrate on startup)
  - `src/MathLLMBackend.Domain` (entities, enums, exceptions)
  - `src/MathLLMBackend.GeolinClient` (Refit client to GeoLin service)
  - `src/MathLLMBackend.ProblemsClient` (Refit client to LLMath-Problems service)

- **Key components**
  - `Program.cs` wires up:
    - CORS from `CorsConfiguration`
    - Logging via NLog
    - Identity API endpoints (`MapIdentityApi<ApplicationUser>()`)
    - Swagger (Development only)
    - DI for Core, DataAccess, GeoLin, Problems clients
    - `WarmupService` which applies EF migrations at startup
  - LLM interaction is encapsulated in `ILlmService` (`LlmService`) using OpenAI-compatible SDK and prompts from config
  - Chats and messages handled by `IChatService` (`ChatService`), including ProblemSolver chat bootstrapping
  - GeoLin adapter via `IGeolinService` (`GeolinService`) using `IGeolinApi` Refit client
  - Problems storage adapter via `IProblemsService` + `IProblemsAPI` Refit client

## Requirements
- .NET 8 SDK
- Docker (for local Postgres, GeoLin, Mongo, Problems service, and optional frontend)
- Optional: Ollama running with an OpenAI-compatible endpoint (defaults in config)

## Quick Start (Docker services)
This repository includes a compose file for the dependent services (DBs, GeoLin, Problems, optional frontend):

```bash
# Start databases, GeoLin, Problems service, and frontend
docker compose up -d
```

Services started by compose:
- Postgres 16 (localhost:5432)
- GeoLin milestones (localhost:7584 -> 8080 in container)
- MongoDB (localhost:27017)
- LLMath-Problems service (localhost:8001)
- Frontend (localhost:3000) pointing to backend at `http://localhost:5000`

Then run the backend (Development):
```bash
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/MathLLMBackend.Presentation
```

Swagger UI (Development):
- `http://localhost:5000/swagger`

## Local Development
1) Install EF CLI (if needed):
```bash
dotnet tool install --global dotnet-ef
```

2) Apply migrations (uses `appsettings.Development.json`):
```bash
export ASPNETCORE_ENVIRONMENT=Development
# From repo root
dotnet ef database update \
  --project src/MathLLMBackend.DataAccess \
  --startup-project src/MathLLMBackend.Presentation
```

3) Run the API:
```bash
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/MathLLMBackend.Presentation
```

## Configuration
Primary configs are in `src/MathLLMBackend.Presentation/appsettings.Development.json` and `appsettings.json`:
- **Database**: `ConnectionStrings:Postgres`
- **Kestrel**: HTTP `http://localhost:5000`, HTTPS `https://localhost:5001`
- **JWT**: `Jwt.Key`, `Jwt.Issuer`, `Jwt.Audience` (used by Identity)
- **CORS**: `CorsConfiguration.Enabled`, `CorsConfiguration.Origin`
- **OpenAi**: models for chat and solver (`Token`, `Url`, `Model`). Defaults point to Ollama (`http://localhost:11434/v1`).
- **DefaultPrompts**: all tutoring prompts (system/user prompts for each task mode, and extraction prompt)
- **GeolinClientOptions.BaseAddress**: defaults to `http://localhost:8080` (compose maps it to 7584 externally)
- **ProblemsClientOptions.BaseAddress**: defaults to `http://localhost:8001/`
- **AdminConfiguration**: default admin credentials (for seeding flows if implemented later)

On startup, `WarmupService` applies migrations automatically (`Database.MigrateAsync()`).

## Domain Model (simplified)
- `ApplicationUser` (extends IdentityUser) with `FirstName`, `LastName`, `StudentGroup`
- `Chat` with `Id`, `Name`, `UserId`, `Type` (Chat/ProblemSolver), `Messages`
- `Message` with `Id`, `ChatId`, `Text`, `CreatedAt`, `MessageType` (User/Assistant/System), `IsSystemPrompt`
- `UserTask` linking a user to a problem with `ProblemId`, `DisplayName`, `TaskType`, `Status`, and optional `AssociatedChatId` and `ProblemHash`

## Controllers and Endpoints (high-level)
Base path uses controller routes; many require Authorization.

- `MapIdentityApi<ApplicationUser>()`
  - Default Identity endpoints (e.g., `POST /register`, `POST /login`, etc.)

- `api/Auth`
  - `POST /api/Auth/register` — Register user via custom flow returning user info

- `api/User` (requires auth)
  - `GET /api/User/me` — Current user profile

- `api/Chat` (requires auth)
  - `POST /api/Chat/create` — Create chat (optionally bound to problem hash and task type Tutor)
  - `GET /api/Chat/get` — List user chats
  - `GET /api/Chat/get/{chatId}` — Chat details (+task type if ProblemSolver)
  - `POST /api/Chat/delete/{id}` — Delete chat

- `api/Message`
  - `POST /api/Message/complete` (requires auth) — Send user message, returns LLM reply text
  - `GET /api/Message/get-messages-from-chat?chatId={guid}` — Messages of a chat (system prompts filtered out)

- `api/v1/Llm`
  - `POST /api/v1/Llm/solve-problem` — One-shot problem solving by LLM
  - `POST /api/v1/Llm/extract-answer` — Extract final answer from a provided solution

- `api/v1/GeolinProxy`
  - `GET /api/v1/GeolinProxy/problem-data?prefix=X&seed=Y` — Find problem by prefix, fetch condition and normalized seed
  - `POST /api/v1/GeolinProxy/check-answer` — Check a user’s answer (hash/attempt/seed/params)

- `api/Tasks` (requires auth)
  - `GET /api/Tasks/problems?page=1&size=10&prefixName=` — Paged problems from GeoLin
  - `POST /api/Tasks/saveProblem?name=&problemHash=&variationCount=1` — Save problems into Problems service
  - `GET /api/Tasks/getSavedProblems` — Get saved problems
  - `GET /api/Tasks/getSavedProblemsByNames?name=` — Get saved problems by name
  - `GET /api/Tasks/getAllNames` — List saved problem type names

- `api/UserTasks` (requires auth)
  - `GET /api/UserTasks?taskType=Tutor` — Get or create user tasks by type
  - `POST /api/UserTasks/{userTaskId}/start` — Mark task InProgress and create/link a chat
  - `POST /api/UserTasks/{userTaskId}/complete` — Mark task as Solved

- `api/Stats`
  - `GET /api/Stats/task-mode-titles` — Titles for task modes from config
  - `GET /api/Stats/user-stats` — Aggregated per-user stats
  - `GET /api/Stats/user-details/{userId}` — Detailed stats for one user

## Example: Register user (Identity)
```bash
curl -X POST \
  'http://localhost:5000/register' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
    "email": "admin@gmail.com",
    "password": "Pwd123!"
  }'
```

For the custom registration returning extended profile:
```bash
curl -X POST \
  'http://localhost:5000/api/Auth/register' \
  -H 'Content-Type: application/json' \
  -d '{
    "email": "admin@gmail.com",
    "password": "Pwd123!",
    "firstName": "Ada",
    "lastName": "Lovelace",
    "studentGroup": "MATH-101"
  }'
```

## LLM Configuration (Ollama/OpenAI-compatible)
The backend uses an OpenAI-compatible client. Defaults are set to use Ollama locally via `http://localhost:11434/v1` and models like `qwen2:0.5b`:
```json
"OpenAi": {
  "ChatModel": { "Token": "ollama", "Url": "http://localhost:11434/v1", "Model": "qwen2:0.5b" },
  "SolverModel": { "Token": "ollama", "Url": "http://localhost:11434/v1", "Model": "qwen2:0.5b" }
}
```
Change these to your OpenAI-compatible provider (Token/Url/Model) as needed.

## CORS
Configure in `appsettings.json`:
```json
"CorsConfiguration": {
  "Enabled": true,
  "Origin": "https://localhost:8080;http://localhost:8080"
}
```
Multiple origins can be separated by `;`.

## Logging
NLog is configured via `src/MathLLMBackend.Presentation/nlog.config`. HTTP logging is enabled and Identity cookie is configured as HttpOnly.

## Troubleshooting
- Ensure Postgres/Mongo services are up (`docker compose ps`) before starting the API.
- Verify `ConnectionStrings:Postgres` matches the compose Postgres (localhost:5432).
- If LLM calls fail, confirm Ollama/OpenAI endpoint and model names.
- Swagger UI is only enabled in Development.

## License
TBD.