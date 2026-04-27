# Greenfield Knowledge Base Solution Architecture

## High Level Design

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                          Greenfield Users                            │
│                    (200 consultants, browser / mobile)               │
└────────────────────────────┬─────────────────────────────────────────┘
                             │ HTTPS
                             ▼
┌──────────────────────────────────────────────────────────────────────┐
│                    Azure Static Web Apps                             │
│              React (TypeScript) SPA — search, authoring              │
└────────────────────────────┬─────────────────────────────────────────┘
                             │ REST + JWT
                             ▼
┌──────────────────────────────────────────────────────────────────────┐
│              Azure App Service (or Container Apps)                   │
│                  ASP.NET Core 10 Web API                             │
│   ArticlesController · Validation · Services · EF Core 9             │
└──────┬─────────────────┬────────────────┬───────────────────┬────────┘
       │                 │                │                   │
       ▼                 ▼                ▼                   ▼
  Azure SQL          Azure Blob       Azure OpenAI        Azure AI
  (SQL Server)       Storage          Service             Search
  articles,          file             embeddings,         vector index,
  categories,        attachments      GPT-4o chat         hybrid search
  tags, audit
       ▲
       │ JWT validation
  Azure AD (Entra ID)
  user identity + groups
```

---

## Technology Choices

### Front End — React (TypeScript)

**Recommendation: React over Blazor.**

React is the right choice for this project for three reasons:

1. **Talent pool.** React has a significantly larger developer community than Blazor. Greenfield's internal team is more likely to find React skills when hiring or training, lowering long term maintenance risk.
2. **Rich-text editing ecosystem.** The article authoring requirement needs a rich text editor. Libraries like TipTap (ProseMirror-based) and Quill have mature React integrations.
3. **Separation of concerns.** A standalone React SPA served from Azure Static Web Apps decouples front-end deployments from API deployments, and enables a CDN-cached shell that loads fast globally.

Blazor would be a reasonable alternative only if Greenfield already had a strong Blazor investment or wanted to share C# component code between API and front end.
### Back End — ASP.NET Core 10, C#

Mandated by the customer (their team will maintain it). Clean, minimal API surface with:
- Controller-based routing for predictable structure
- FluentValidation for expressive, testable input rules
- EF Core 9 + SQL Server for the primary store

### Database — Azure SQL (SQL Server)

The customer already owns a SQL Server licence, making Azure SQL the zero-additional-cost choice. Key benefits for this scenario:
- Full text Search (`CONTAINS` / `FREETEXT`) for keyword search without a separate search tier
- Row-level security and fine grained permissions align with the access control requirements
- EF Core migration support with strong tooling

*SQLite is used in the code for local development portability only.*

### Authentication — Azure AD (Entra ID)

Mandated. JWT Bearer middleware validates tokens issued by Entra ID. The oid claim is used as the stable user identifier in audit records. Role-based access control maps Entra ID groups to application roles (Reader, Author, Admin).

### File Storage — Azure Blob Storage

Article attachments (Word docs, PDFs, images) are stored in Blob Storage with pre-signed SAS URLs returned by the API.

---

## AI Integration Design

### Smart Search

The goal is for a query like *"how to handle difficult stakeholders"* to surface an article titled *"Client Relationship Management Strategies"* even when there is no keyword overlap.

**Recommended approach: Hybrid search (BM25 keyword + vector similarity)**

#### How it works

1. **Indexing (on article create/update):**
   - Call Azure OpenAI `text-embedding-3-small` with the article title + first ~500 tokens of body.
   - Store the resulting 1,536-dimension float vector in **Azure AI Search** alongside the article ID and key metadata fields.

2. **At search time:**
   - Run the user's query through two channels in parallel:
     - **Keyword channel:** SQL Server Full-Text Search (`CONTAINS`) against the primary database. It's fast, exact, and handles typos via stemming.
     - **Semantic channel:** Embed the query with `text-embedding-3-small`, run a vector nearest-neighbour search in Azure AI Search, retrieve the top-K article IDs ranked by cosine similarity.
   - **Merge and re-rank:** Use Reciprocal Rank Fusion (RRF) to combine both result lists. Azure AI Search's built-in hybrid mode does this automatically.
   - Apply quality signal boosting: articles with higher upvote counts and view counts receive a small ranking bonus.

#### Why not pure keyword or pure vector?

| Approach | Strength | Weakness |
|---|---|---|
| SQL keyword only | Fast, cheap, exact | Misses synonyms and intent |
| Vector only | Understands intent | Can hallucinate; no exact match guarantee |
| **Hybrid (recommended)** | Best of both | Slightly more infrastructure |

For 2,000 articles and 200 users, Azure AI Search's free or Basic tier is sufficient — no need for the expensive Standard tier.

### Auto-Categorisation

When an author creates or edits an article:
1. The front end calls `POST /api/articles/suggest-category` with the title and first 200 words of the body.
2. The API calls Azure OpenAI `gpt-4o` with a structured prompt: *"Given these categories: [list], which one best fits this article? Respond with only the category name."*
3. The suggestion is returned to the front end and pre-selected in the category dropdown — the author can override it.
4. No auto-assignment without user confirmation.

---

## Data Model

```
Category
  id (PK)
  name
  description

Article
  id (PK)
  title
  body                  -- rich text (HTML or Markdown)
  category_id (FK)
  author_id             -- Azure AD OID
  author_name
  view_count
  upvote_count
  is_deleted            -- soft delete
  created_at
  updated_at

Tag
  id (PK)
  name (unique, lowercase)

ArticleTag  [junction]
  article_id (FK)
  tag_id     (FK)

Upvote
  id (PK)
  article_id (FK)
  user_id               -- Azure AD OID
  created_at
  [UNIQUE on (article_id, user_id)]
```

**Key relationships:**
- Article -> Category: many-to-one
- Article <-> Tag: many-to-many via ArticleTag
- Article <- Upvote: one-to-many (with unique user constraint)

---

## MVP Scope — 4-Week Plan

### What we build (MVP)

| Week | Focus | Deliverables |
|------|-------|-------------|
| 1 | Foundation | Entra ID auth wired up; DB schema + EF migrations; CRUD API (create, read, list, delete); React app shell with login |
| 2 | Core UX | Article authoring with rich-text editor; category + tag UI; keyword search (SQL FTS); search results page |
| 3 | AI features | Semantic search (Azure AI Search + embeddings); auto-categorisation suggestion; quality signals (upvotes, view counts visible in UI) |
| 4 | Polish + UAT | Audit log viewer for admins; access control enforcement (author-edit / admin-delete); performance testing; UAT with Greenfield stakeholders; bug fixes |

### What we defer (post-MVP)

- **File attachments** — the UX is valuable but storage + virus scanning adds complexity. Phase 2.
- **Bulk import of 2,000 Word docs** — separate migration workstream. Requires parsing, deduplication, and manual review. Can run in parallel with development.
- **AI-powered article quality scoring** — enriching the search ranking with an LLM-based quality score.
- **Mobile-responsive design polish** — the React app will be functional on mobile, but pixel-perfect responsive styling is deferred.
- **Analytics dashboard** — view counts, search term trends, popular articles. Phase 2.

### Rationale for deferral

With 200 users and a 4-week window, the highest value is getting consultants off the shared drive and into a searchable system. File attachments and analytics are useful but don't block the core use case. The AI features are achievable in Week 3 because Azure AI Search handles the heavy lifting.
