# Architecture & Design — Carrier Onboarding Assistant

This document explains **what the tool does**, **how it works**, and **the technology behind it**,
with diagrams. The GitHub Markdown renderer draws the `mermaid` code blocks as diagrams.

---

## 1. The problem & the idea

Onboarding a new shipping carrier means an engineer reads a long API integration guide and
hand-writes a C# integration (client, request/response models, field mappings, error mapping,
tests). It is slow and repetitive.

A plain LLM can't be trusted to do this: it will **hallucinate** endpoints, field names and
error codes that aren't in the carrier's doc. The fix is **Retrieval-Augmented Generation (RAG)**:
we ground every answer in the *actual* document and force the model to use only retrieved facts,
flagging anything uncertain with `// TODO: verify`.

> The tool **never deploys**. It only writes **DRAFT** files for a human to review.

---

## 2. What is RAG (in one picture)

```mermaid
flowchart LR
	Q[Question or<br/>generation task] --> E[Embed as vector]
	E --> S[(Vector store)]
	S -->|cosine similarity| R[Top-K relevant chunks]
	R --> P[Prompt:<br/>system rules + chunks + task]
	P --> L[LLM]
	L --> A[Grounded output<br/>with citations]
```

- **Embedding** turns text into a vector (array of numbers) that captures *meaning*.
- Similar meaning → similar vectors → high **cosine similarity**.
- We retrieve only the most relevant chunks and tell the LLM to answer **only** from them.

---

## 3. High-level system architecture

```mermaid
flowchart TB
	subgraph CLI["CarrierOnboarding.Cli (console app)"]
		PROG[Program.cs<br/>System.CommandLine]
		KF[KernelFactory<br/>provider wiring]
		CFG[appsettings.json +<br/>user-secrets / env vars]
	end

	subgraph CORE["CarrierOnboarding.Core (library)"]
		direction TB
		subgraph ING["Ingestion"]
			EX[IDocumentTextExtractor<br/>PDF / DOCX / TXT / MD]
			CH[ITextChunker<br/>TokenTextChunker]
		end
		subgraph RET["Retrieval"]
			EMB[IEmbeddingService]
			VS[IVectorStore<br/>InMemoryVectorStore]
		end
		subgraph SVC["Services"]
			IDX[IndexingService]
			ASK[AskService]
			GEN[GenerationService]
			EVAL[EvaluationService]
		end
		PB[PromptBuilder /<br/>GenerationPrompts]
		CHAT[IChatService]
	end

	subgraph PROVIDERS["LLM Providers (Semantic Kernel)"]
		AOAI[Azure OpenAI]
		OLL[Ollama local]
	end

	PROG --> IDX & ASK & GEN & EVAL
	KF --> CHAT & EMB
	CFG --> KF
	IDX --> EX --> CH --> EMB --> VS
	ASK --> EMB --> VS --> PB --> CHAT
	GEN --> EMB --> VS --> PB --> CHAT
	CHAT --> AOAI & OLL
	EMB --> AOAI & OLL
```

**Why this shape?** `Core` depends only on **interfaces**. The CLI injects the concrete
LLM provider and the vector store. That keeps the business logic provider-agnostic and lets
us swap the vector store (in-memory → SQLite/Qdrant) by changing one registration.

---

## 4. The four commands as data flows

### 4.1 `index` — ingest a carrier document

```mermaid
flowchart LR
	D[Doc: pdf/docx/md] --> X[Extract text<br/>+ page numbers]
	X --> C[Chunk ~800 tokens<br/>100 overlap<br/>keep headings]
	C --> M[Embed each chunk]
	M --> U[(Vector store<br/>tagged by carrier)]
	U --> J[(Persist to<br/>.carrier-index/index.json)]
```

Key detail: **overlap** means consecutive chunks share ~100 tokens, so a sentence split
across a boundary is never lost. Each chunk remembers its **section heading** and **page**
for citations.

### 4.2 `ask` — grounded Q&A with citations

```mermaid
sequenceDiagram
	participant U as User
	participant A as AskService
	participant E as IEmbeddingService
	participant V as IVectorStore
	participant P as PromptBuilder
	participant L as IChatService (LLM)

	U->>A: ask(carrier, question)
	A->>E: embed(question)
	E-->>A: query vector
	A->>V: search(carrier, vector, topK=5)
	V-->>A: top-5 chunks + scores
	alt no chunks
		A-->>U: "not found in document"
	else chunks found
		A->>P: build prompt (rules + chunks + question)
		P-->>A: user prompt
		A->>L: complete(system, user)
		L-->>A: grounded answer
		A-->>U: answer + cited sources
	end
```

The **system prompt** forces: *use only the excerpts, cite section/page, say
"not found in document" if absent, never invent values.*

### 4.3 `generate` — draft an integration (one LLM call per file)

```mermaid
flowchart TB
	START[generate carrier,<br/>examples, out] --> T{Fixed retrieval<br/>queries}
	T --> T1[authentication]
	T --> T2[request fields]
	T --> T3[response/label format]
	T --> T4[error codes]
	T --> T5[rate limits]
	T --> T6[sandbox endpoints]
	T1 & T2 & T3 & T4 & T5 & T6 --> CTX[Per-topic chunks]
	EXF[Load 1-2 example<br/>integration files] --> CALL
	CTX --> CALL[One focused LLM call PER FILE]
	CALL --> F1[CarrierClient.cs]
	CALL --> F2[CarrierModels.cs]
	CALL --> F3[CarrierFieldMapping.cs]
	CALL --> F4[CarrierErrorMapping.cs]
	CALL --> F5[CarrierClientTests.cs]
	CTX --> RPT[MAPPING_REPORT.md<br/>fields + OPEN QUESTIONS]
```

- **Few-shot examples** (`--examples`) teach the model your house naming/logging/exception style.
- **One file per call** keeps each prompt small, focused and accurate.
- **`MAPPING_REPORT.md`** lists each carrier field, your source field, the doc section it came
  from, and an **OPEN QUESTIONS** section for anything unclear — nothing is invented.

### 4.4 `evaluate` — grade a draft (no LLM)

```mermaid
flowchart LR
	G[Generated folder] --> GP[Extract property names<br/>+ error codes via regex]
	R[Reference folder] --> RP[Extract property names<br/>+ error codes via regex]
	GP --> CMP{Compare}
	RP --> CMP
	CMP --> MF[Matched / Missed fields]
	CMP --> MC[Matched / Missed error codes]
	MF & MC --> COV[Coverage % report]
```

Pure static string analysis — deterministic and offline, so it needs **no LLM provider**.

---

## 5. Request/response model mapping (what `generate` reasons about)

```mermaid
flowchart LR
	subgraph OURS["Our canonical shipment model"]
		O1[Shipment.Sender.*]
		O2[Shipment.Recipient.*]
		O3[Shipment.Parcel.*]
		O4[Shipment.LabelFormat]
	end
	subgraph DOC["Carrier doc (retrieved chunks)"]
		C1[ship_from.*]
		C2[ship_to.*]
		C3[parcel.*]
		C4[label_format]
	end
	O1 -.map.-> C1
	O2 -.map.-> C2
	O3 -.map.-> C3
	O4 -.map.-> C4
	OURS --> FM[CarrierFieldMapping.cs<br/>+ MAPPING_REPORT.md]
	DOC --> FM
```

---

## 6. The technology behind it

| Concern | Technology | Why |
|---|---|---|
| CLI parsing | **System.CommandLine** | Typed options, subcommands, built-in `--help` |
| LLM orchestration | **Microsoft Semantic Kernel** | One abstraction over chat + embeddings |
| LLM providers | **Azure OpenAI** / **Ollama** | Cloud or fully local; selectable in config |
| PDF text | **UglyToad.PdfPig** | Reliable per-page text extraction |
| Word text | **DocumentFormat.OpenXml** | First-party `.docx` parsing |
| Vectors | **In-memory cosine store** (+ JSON persistence) | Simple start; swappable behind `IVectorStore` |
| Config & DI | **Microsoft.Extensions.*** | Options binding, user-secrets, logging, DI |
| Tests | **xUnit** | Chunking, retrieval (fake embedder), prompts, eval |

### How similarity is computed (cosine)

Two vectors `a` and `b`:

```
similarity = (a · b) / (‖a‖ × ‖b‖)
```

- `a · b` is the dot product (overlap of meaning).
- Dividing by the magnitudes makes it **length-independent** — only *direction* (meaning) matters.
- Result in `[0, 1]`; higher = more relevant. We sort chunks by this and take the top-K.

### Chunking math (why ~800/100)

- LLMs and embeddings have token limits and lose precision on very long text.
- ~**800 tokens** ≈ a focused, self-contained passage.
- **100-token overlap** preserves context across boundaries.
- Tokens are estimated as `chars / 4` (a good cross-model heuristic) in `TokenEstimator`.

---

## 7. Guardrails that keep output trustworthy

```mermaid
flowchart TB
	IN[Retrieved chunks only] --> R1[System prompt:<br/>'use ONLY these facts']
	R1 --> R2[Follow example code<br/>interfaces/naming EXACTLY]
	R2 --> R3[Mark uncertainty:<br/>// TODO: verify]
	R3 --> R4[Unclear items go to<br/>OPEN QUESTIONS]
	R4 --> OUT[DRAFT files for<br/>human review]
```

1. **Grounding** — only retrieved document text is used as source of carrier facts.
2. **Pattern fidelity** — few-shot examples enforce your coding conventions.
3. **Explicit uncertainty** — `// TODO: verify` + an OPEN QUESTIONS list; nothing invented.
4. **Human-in-the-loop** — everything is a draft; the tool never deploys.

---

## 8. Project layout recap

```
src/
  CarrierOnboarding.Core/     Abstractions, Ingestion, Vector, Llm, Prompts, Services, DI
  CarrierOnboarding.Cli/      Program.cs (commands), KernelFactory, appsettings.json
tests/
  CarrierOnboarding.Tests/    Chunking, retrieval (fake embedder), prompt building, evaluation
samples/
  docs/      acmeship-api-guide.md  (fictional carrier doc)
  examples/  ZipShip* .cs           (house pattern / evaluation reference)
```

See **[HOW_TO_RUN.md](HOW_TO_RUN.md)** for setup and example commands, and
**[README.md](README.md)** for a feature overview.
