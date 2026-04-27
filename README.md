# Greenfield Knowledge Base — Submission

## Running the API

Prerequisites: .NET 10 SDK

```bash
cd src/KnowledgeBase/KnowledgeBase.Api
dotnet run
```

The API starts at `http://localhost:5XXX`. Because `AzureAd.TenantId` is empty in `appsettings.json`, authentication is skipped and all endpoints are open in development.

OpenAPI spec is served at `/openapi/v1.json`.

### Quick smoke test

```bash
# Create an article
curl -X POST http://localhost:5XXX/api/articles \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Client Relationship Management Strategies",
    "body": "Maintaining trust with difficult stakeholders requires active listening and clear escalation paths.",
    "tags": ["stakeholders", "client-management"],
    "categoryId": null
  }'

# Search
curl "http://localhost:5XXX/api/articles?search=stakeholder"

# Upvote
curl -X POST http://localhost:5XXX/api/articles/{id}/upvote
```

---

## Running the Tests

```bash
cd src/KnowledgeBase
dotnet test
```

The test suite includes **5 tests across 2 projects (2 unit tests and 3 integration tests)**, all using an isolated in-memory SQLite database — no external dependencies needed.

To run a specific test by name:

```bash
dotnet test --filter "CreateAsync_ValidRequest_PersistsArticleWithNormalisedTags"
```

---

## What I'd Do With More Time

- **EF Core migrations** instead of `EnsureCreated()` for production-safe schema evolution
- **SQL Server Full-Text Search** in production (currently SQLite `LIKE`) — the `IArticleService.SearchAsync` implementation is structured so only the EF query changes, not the service interface
- **Azure AI Search integration** in `AzureOpenAISemanticSearchService` — the interface and wiring are in place. 
- **Role-based access control** — the `[Authorize]` attributes are ready; the role mappings from Entra ID groups to application roles need configuration
- **Health check endpoint** (`/health`) wired to EF Core and dependencies — required for the CI/CD smoke tests in the pipeline
- **`PUT /api/articles/{id}`** (edit) — not in the required endpoints but an obvious gap for a real knowledge base
- **Soft-delete endpoint** (`DELETE /api/articles/{id}`) with admin-only authorization

---

## AI Tools Used

- **Claude (claude.ai)** — used to scaffold the solution structure, generate boilerplate (csproj references, NuGet add commands), draft the architecture and review the customer recommendation for clarity and tone.