# How to Run the Carrier Onboarding Assistant

This guide walks you through running the tool end to end. All commands are shown for
**PowerShell** and are run from the repository root.

```powershell
cd C:\Raj\Personal\CarrierOnBoardingAssistant\CarrierOnBoardingAssistant
```

---

## 1. Prerequisites

- **.NET 8 SDK** — verify with `dotnet --version` (should be `8.x`).
- **One LLM provider** (needed for `index`, `ask`, `generate`; `evaluate` needs none):
  - **Ollama** (local, no API key), or
  - **Azure OpenAI** (chat + embedding deployments).

---

## 2. Choose and configure a provider

Provider settings live in `src/CarrierOnboarding.Cli/appsettings.json`.
**Never put secrets in that file** — use user-secrets or environment variables.

### Option A — Ollama (local, no keys)

1. Install Ollama from https://ollama.com and start it.
2. Pull the models used by the default config:

   ```powershell
   ollama pull llama3.1
   ollama pull nomic-embed-text
   ```

3. Ensure `appsettings.json` has:

   ```jsonc
   "Llm": { "Provider": "Ollama" }
   ```

### Option B — Azure OpenAI

1. Set `appsettings.json`:

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

2. Provide the API key out-of-band:

   ```powershell
   # Preferred: user-secrets
   dotnet user-secrets --project src/CarrierOnboarding.Cli set "Llm:AzureOpenAI:ApiKey" "<your-key>"

   # Or an environment variable
   $env:AZURE_OPENAI_API_KEY = "<your-key>"
   ```

---

## 3. Build and test

```powershell
dotnet build
dotnet test
```

You should see a successful build and **15 passing tests**.

---

## 4. Run the commands

Tip: `--` separates `dotnet run` arguments from the tool's own arguments.

```powershell
$cli = "src/CarrierOnboarding.Cli"
```

### See all commands

```powershell
dotnet run --project $cli -- --help
```

### 1) index — read a carrier doc into the vector store

```powershell
dotnet run --project $cli -- index --doc samples/docs/acmeship-api-guide.md --carrier AcmeShip
```

Supported inputs: `.pdf`, `.docx`, `.txt`, `.md`. The index is persisted to
`./.carrier-index/index.json` so later commands can use it.

### 2) ask — grounded Q&A with citations

```powershell
dotnet run --project $cli -- ask --carrier AcmeShip --question "How do I authenticate?"
```

Answers only from the retrieved chunks and cites section/page, or replies
`not found in document`.

### 3) generate — draft an integration (DRAFT only)

```powershell
dotnet run --project $cli -- generate --carrier AcmeShip --examples samples/examples --out out/AcmeShip
```

Writes draft `*.cs` files plus `MAPPING_REPORT.md` into `out/AcmeShip`.
**Review every `// TODO: verify` and the OPEN QUESTIONS section before using.**

### 4) evaluate — compare a draft against a reference (no LLM needed)

```powershell
dotnet run --project $cli -- evaluate --carrier AcmeShip --generated out/AcmeShip --reference samples/examples
```

Reports field and error-code coverage (matched vs missed).

---

## 5. Quick end-to-end smoke test (no LLM)

This verifies the build and the evaluation pipeline without any provider:

```powershell
dotnet build
dotnet run --project src/CarrierOnboarding.Cli -- evaluate --carrier AcmeShip --generated samples/examples --reference samples/examples
```

Expected: `Field coverage: 100%` and `Error-code coverage: 100%`.

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `Azure OpenAI API key not found` | Set user-secret `Llm:AzureOpenAI:ApiKey` or `AZURE_OPENAI_API_KEY`. |
| Connection refused on `index`/`ask` with Ollama | Make sure `ollama serve` is running and models are pulled. |
| `No indexed content found for carrier` | Run the `index` command first for that carrier. |
| Empty/odd answers | Re-run `index`; confirm the embedding model is pulled/deployed. |
