# Carrier Onboarding Assistant

A developer tool that uses **Retrieval-Augmented Generation (RAG)** to read a shipping
carrier's API integration guide and generate a **DRAFT** C# carrier integration that
follows our existing PBServices code patterns.

> ⚠️ This tool **never deploys anything**. It only writes draft files and reports for an
> engineer to review. Every generated file is a starting point, not production code.

## Documentation

- **[HOW_TO_RUN.md](HOW_TO_RUN.md)** — setup and step-by-step commands to run the tool.
- **[ARCHITECTURE.md](ARCHITECTURE.md)** — how it works, data-flow diagrams, and the tech behind it.

## Solution layout

```
src/
  CarrierOnboarding.Core   Ingestion, chunking, retrieval, generation & evaluation services (+ interfaces)
  CarrierOnboarding.Cli    System.CommandLine console app wiring the commands to the core
tests/
  CarrierOnboarding.Tests  xUnit tests (chunking, retrieval with a fake embedder, prompt building, evaluation)
samples/
  docs/      A small fictional carrier API guide (AcmeShip) to try end to end
  examples/  An example house integration (ZipShip) used as a few-shot pattern / evaluation reference
```

## Prerequisites

- .NET 8 SDK
- One LLM provider:
  - **Azure OpenAI** with a chat deployment and an embedding deployment, **or**
  - **[Ollama](https://ollama.com)** running locally with a chat model and an embedding model.

## Configuration

Provider selection lives in [`src/CarrierOnboarding.Cli/appsettings.json`](src/CarrierOnboarding.Cli/appsettings.json).
**No API keys are stored in source** — provide them via user-secrets or environment variables.

### Option A: Ollama (local, no keys)

```jsonc
"Llm": {
  "Provider": "Ollama",
  "Ollama": {
	"Endpoint": "http://localhost:11434",
	"ChatModel": "llama3.1",
	"EmbeddingModel": "nomic-embed-text"
  }
}
```

Pull the models once:

```powershell
ollama pull llama3.1
ollama pull nomic-embed-text
```

### Option B: Azure OpenAI

```jsonc
"Llm": {
  "Provider": "AzureOpenAI",
  "AzureOpenAI": {
	"Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
	"ChatDeployment": "gpt-4o",
	"EmbeddingDeployment": "text-embedding-3-small"
  }
}
```

Provide the key out-of-band (never in `appsettings.json`):

```powershell
# user-secrets (preferred for local dev)
cd src/CarrierOnboarding.Cli
dotnet user-secrets set "Llm:AzureOpenAI:ApiKey" "<your-key>"

# or an environment variable
$env:AZURE_OPENAI_API_KEY = "<your-key>"
```

## Build & test

```powershell
dotnet build
dotnet test
```

## Commands

Run from the repo root (`--` separates `dotnet run` args from the tool's args):

```powershell
$cli = "src/CarrierOnboarding.Cli"
```

### 1. index — read a doc into the vector store

```powershell
dotnet run --project $cli -- index --doc samples/docs/acmeship-api-guide.md --carrier AcmeShip
```

Extracts text (PDF via PdfPig, `.docx` via OpenXml, `.txt`/`.md` as plain text), splits it
into ~800-token chunks with 100-token overlap keeping section headings as metadata, embeds
them, and stores them tagged with the carrier name. The index is persisted to
`./.carrier-index/index.json` so later commands can use it.

### 2. ask — grounded Q&A with citations

```powershell
dotnet run --project $cli -- ask --carrier AcmeShip --question "How do I authenticate?"
```

Retrieves the top 5 chunks and answers **only** from them, citing the section/page.
If the answer is not in the document, it replies `not found in document`.

### 3. generate — draft an integration

```powershell
dotnet run --project $cli -- generate --carrier AcmeShip --examples samples/examples --out out/AcmeShip
```

Runs fixed retrieval queries (authentication, request fields, response/label format, error
codes, rate limits, sandbox endpoints), loads 1–2 example integrations from `--examples`
as few-shot pattern examples, then makes a focused LLM call **per file** to produce:

- `AcmeShipClient.cs`
- `AcmeShipModels.cs` (request/response models)
- `AcmeShipFieldMapping.cs` (our shipment model → carrier fields)
- `AcmeShipErrorMapping.cs`
- `AcmeShipClientTests.cs` (xUnit stubs)
- `MAPPING_REPORT.md` — each carrier field, our source field, the doc section it came from,
  and an **OPEN QUESTIONS** list where the doc was unclear. Uncertain items are marked with
  `// TODO: verify` and nothing is invented.

### 4. evaluate — compare a draft to a reference

```powershell
dotnet run --project $cli -- evaluate --carrier AcmeShip --generated out/AcmeShip --reference samples/examples
```

Statically compares the generated draft against a hand-written integration and reports which
fields and error codes matched or were missed. This command needs no LLM provider.

## Design notes

- **Interfaces everywhere.** `IDocumentTextExtractor`, `ITextChunker`, `IEmbeddingService`,
  `IVectorStore`, `IChatService` and the service interfaces live in `CarrierOnboarding.Core`.
- **Swappable vector store.** The default is an in-memory cosine-similarity store with optional
  JSON persistence. Replace the single `IVectorStore` registration to move to SQLite or Qdrant.
- **Prompting rules.** The system prompts cast the model as a senior .NET integration engineer
  that must follow the example code exactly, use only retrieved facts, and mark uncertainty with
  `// TODO: verify`. Each generation call is scoped to one file and includes the retrieved chunks
  with their section titles.
