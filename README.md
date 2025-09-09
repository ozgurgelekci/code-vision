# 🔍 CodeVision - AI Destekli Kod İnceleme ve Refaktör Asistanı

GitHub PR'larını otomatik analiz eden; Roslyn ve (opsiyonel) GPT ile özet/öneri üreten, arka planda kuyruk ile çalışan ve Blazor UI'da sonuçları gösteren servis.

## 🚀 Güncel Özellikler

- GitHub Webhook ile otomatik tetikleme (`opened/synchronize/reopened/closed`)
- Gerçek PR diff'ini GitHub API'den alma (Accept: `application/vnd.github.v3.diff`)
- Roslyn tabanlı statik analiz (syntax-only diagnostics; referans kaynaklı gürültü azaltıldı)
- GPT destekli özet ve öneriler (İngilizce promptlar; Summary 12k, Issues 10k, Code 8k; fallback 500)
- Arka plan işleyici (queue + worker)
- Blazor Server UI + SignalR gerçek zamanlı bildirimler (toast + ses + countdown)
  - Eventler: `NewPullRequest`, `AnalysisUpdated`, `AnalysisCompleted`, `PullRequestClosed`
  - InProgress kartları info (mavi) arka plan; `Details` butonu Completed olana kadar devre dışı
- Kalite skoru (0-100)


## 🏗️ Mimari (Özet)
```
GitHub → Webhook → Queue → Background Worker → PostgreSQL → API → Blazor UI
                                   ↳ Roslyn / GPT Analizi
```

## 📦 Proje Yapısı
- `CodeVision.API`: REST API (Swagger açık)
- `CodeVision.Core`: Domain & arayüzler
- `CodeVision.Infrastructure`: EF Core, servisler, queue
- `CodeVision.UI`: Blazor Server UI

## ⚡ Hızlı Başlangıç (Docker Compose - önerilen)
```bash
docker-compose up -d
```

## ⚡ Hızlı Başlangıç (Docker - manuel)
```bash
# Ağ
docker network create codevision-network

# PostgreSQL
docker run -d --name codevision_postgres --network codevision-network \
  -e POSTGRES_USER=codevision_user \
  -e POSTGRES_PASSWORD=your_password \
  -e POSTGRES_DB=codevision_db \
  -p 5433:5432 postgres:16

# API (önce image'ı build edin: Dockerfile)
docker run -d --name codevision_api --network codevision-network -p 5001:8080 \
  -e "ConnectionStrings__DefaultConnection=Host=codevision_postgres;Port=5432;Database=codevision_db;Username=codevision_user;Password=your_password" \
  code-vision-codevision-api

# UI (önce image'ı build edin: Dockerfile.ui)
docker run -d --name codevision_ui --network codevision-network -p 3001:8080 \
  -e ApiSettings__BaseUrl=http://codevision_api:8080 \
  code-vision-codevision-ui
```

## 🛠️ Development
```bash
# API
cd CodeVision.API && dotnet run

# UI
cd CodeVision.UI && dotnet run
```

## 🔧 Konfigürasyon (Örnek)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Port=5432;Database=codevision_db;Username=...;Password=..."
  },
  "OpenAI": { "ApiKey": "<your-openai-key>", "Model": "gpt-4" },
  "GitHub": { "WebhookSecret": "<your-webhook-secret>", "Token": "<github-token>", "ApiUrl": "https://api.github.com" }
}
```

UI, API adresini `ApiSettings__BaseUrl` ile alır (örn. Railway API URL'si). Detay özeti (Summary) HTML render edilir (sanitize edilir).

## 📊 API Endpoints (Güncel)
- `POST /webhook/github`
- `GET /api/dashboard`
- `GET /api/analyses`
- `GET /api/analyses/{id}`
- `GET /health`
 - SignalR Hub: `/hubs/analysis` (NewPullRequest, AnalysisUpdated, AnalysisCompleted, PullRequestClosed)

## 🎯 Skor & Risk
- Roslyn Skoru: 100 − (Error×10 + Warning×5 + Info×1)
- GPT Skoru: 100 − (Critical×15 + High×10 + Medium×5 + Low×2)
- Toplam: 60% Roslyn + 40% GPT

Risk: High / Medium / Low (Roslyn bulgularının şiddetine göre)

## 🚀 Production (Örnek)
- Railway: Git push → auto-deploy
- Docker: `docker build` + `docker run`

> Örnek değerler placeholder'dır. Gerçek anahtarları paylaşmayın.

-----

# 🔍 CodeVision - AI‑powered Code Review Assistant

Automatically analyzes GitHub PRs; produces Roslyn/GPT insights; processes jobs in background; displays results in a Blazor UI.

## 🚀 Features (Current)
- GitHub webhook trigger (`opened/synchronize/reopened/closed`)
- Fetch real PR diff from GitHub API (Accept: `application/vnd.github.v3.diff`)
- Roslyn static analysis (syntax-only diagnostics to reduce reference noise)
- Optional GPT-based summary and suggestions (English prompts; Summary 12k, Issues 10k, Code 8k; fallback 500)
- Background queue + worker
- Blazor Server UI with SignalR real-time notifications (toast + sound + countdown)
  - Events: `NewPullRequest`, `AnalysisUpdated`, `AnalysisCompleted`, `PullRequestClosed`
  - InProgress cards use info (blue) background; `Details` is enabled only when Completed
- Quality score (0-100)

## 🏗️ Architecture
```
GitHub → Webhook → Queue → Background Worker → PostgreSQL → API → Blazor UI
                               ↳ Roslyn / GPT
```

## ⚡ Quick Start (Docker Compose - recommended)
```bash
docker-compose up -d
```

## ⚡ Quick Start (Docker - manual)
```bash
docker network create codevision-network

docker run -d --name codevision_postgres --network codevision-network \
  -e POSTGRES_USER=codevision_user \
  -e POSTGRES_PASSWORD=your_password \
  -e POSTGRES_DB=codevision_db \
  -p 5433:5432 postgres:16

docker run -d --name codevision_api --network codevision-network -p 5001:8080 \
  -e "ConnectionStrings__DefaultConnection=Host=codevision_postgres;Port=5432;Database=codevision_db;Username=codevision_user;Password=your_password" \
  code-vision-codevision-api

docker run -d --name codevision_ui --network codevision-network -p 3001:8080 \
  -e ApiSettings__BaseUrl=http://codevision_api:8080 \
  code-vision-codevision-ui
```

## 🔧 Configuration (Samples)
```json
{
  "ConnectionStrings": { "DefaultConnection": "Host=...;Port=5432;Database=codevision_db;Username=...;Password=..." },
  "OpenAI": { "ApiKey": "<your-openai-key>", "Model": "gpt-4" },
  "GitHub": { "WebhookSecret": "<your-webhook-secret>", "Token": "<github-token>", "ApiUrl": "https://api.github.com" }
}
```

## 📊 API Endpoints
- `POST /webhook/github`
- `GET /api/dashboard`
- `GET /api/analyses`
- `GET /api/analyses/{id}`
- `GET /health`

## 📝 Notes
- UI updates in real-time via SignalR; no manual refresh required
- Keep secrets in environment variables (do not commit)

## 📄 License
MIT
