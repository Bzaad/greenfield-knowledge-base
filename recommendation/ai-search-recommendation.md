# AI-Powered Search: Technical Recommendation for Greenfield Consulting

**Prepared for:** Greenfield Consulting CTO
**Context:** Internal Knowledge Base — AI search feature

---

## What We Recommend

We recommend a **hybrid search** approach that combines two techniques: traditional keyword matching and AI-powered semantic understanding.

Here is how it works in plain terms:

When a consultant searches for *"how to handle difficult stakeholders"*, the system does two things simultaneously:

1. **Keyword search** — it looks for articles that literally contain words like "stakeholders", "difficult", or "handle". This is fast and accurate for exact matches.

2. **AI semantic search** — it uses an AI model to understand the *meaning* of the query, then finds articles with similar meaning even if they use completely different words. An article titled *"Client Relationship Management Strategies"* would rank highly for that search, even though none of those words appear in the query.

The results from both channels are merged and ranked together. Articles that have been upvoted often or read frequently get a small additional boost — so the most trusted content surfaces first.

---

## What It Will Do Well

- **Intent-aware search:** Consultants can search the way they think, not just with exact keywords. "Project went over budget" will find "Cost Control in Fixed-Price Engagements".
- **Handles jargon and synonyms:** "KPIs", "metrics", and "performance indicators" are understood as related.
- **Improves over time:** As more articles are added and upvoted, search quality naturally improves because quality signals get richer.
- **Auto-category suggestions:** When an author writes a new article, the AI suggests the right category. The author confirms or overrides.\


## What It Will Not Do Well

- **Highly specific technical queries:** If a consultant searches for a very specific code snippet or contract clause number, keyword search will always be more reliable than the AI component. The hybrid approach means keyword results are still included, so this is managed.
- **Brand new topics:** If an article on a brand-new subject area has just been added and no similar content exists, the AI may not find strong semantic matches. This improves as the knowledge base grows.
- **Perfect accuracy:** AI search surfaces *probably relevant* results — it is not 100% precise. Occasionally an irrelevant article will appear in results. This is normal and expected behaviour for semantic search.
- **Real-time updates:** There is a small delay (typically seconds) between an article being published and it being searchable via the AI channel. The keyword channel is immediate.

---

## Cost Implications

Azure OpenAI charges per token in **USD**(roughly per word). For your scale:

| Activity | Volume | Estimated Cost |
|---|---|---|
| One-time: embed existing 2,000 docs | ~1M tokens | **~$0.02** |
| Daily: 200 users × 20 searches | ~80,000 tokens/day | **~$0.55$/month** |
| Daily: AI category suggestions (authors only) | ~20,000 tokens/day | **~$0.14/month** |
| Azure AI Search (Basic) | Fixed Infrastructure | **~$74.00/month**|
| **Total Monthly AI Cost** || **~$75.00/month** |

---

## Risks and Caveats

**1. Data stays within your Azure tenant**
All AI processing uses your existing Azure OpenAI resource. Article content is never sent to OpenAI's public service. This is important given that some articles may be client-sensitive.

**2. The AI is a tool, not an editor**
Auto-categorisation is a *suggestion*, not a decision. We will build the system so that authors always confirm or override the suggested category before publishing.

**3. Search quality depends on article quality**
If articles are poorly titled, or inconsistently structured, search quality will be lower, for both keyword and AI search.

**4. Model changes over time**
Microsoft periodically updates the Azure OpenAI models. When the embedding model changes, all existing articles would need to be reindexed. This is a one-time automated process taking roughly an hour at this scale, but it requires planning.

**5. This is MVP-grade AI, not research-grade**
The hybrid approach we are recommending is production-proven and used by major enterprise platforms. It is not experimental. However, it is also not a custom-trained model as it uses Azure's general-purpose embedding models, which perform well for English-language professional content but have not been trained specifically on consulting industry knowledge.

---

## Our Recommendation in One Sentence

Hybrid search using Azure AI Search and Azure OpenAI is the right approach for this project — it is affordable, well-supported, keeps your data within your Azure tenant, and will materially improve how 200 consultants find knowledge compared to the shared drive today.
