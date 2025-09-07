# 🔍 CodeVision - AI Destekli Kod İnceleme ve Refaktör Asistanı

GitHub PR'larını otomatik analiz eden; Roslyn ve (opsiyonel) GPT ile özet/öneri üreten, arka planda kuyruk ile çalışan ve Blazor UI'da sonuçları gösteren servis.

## 🚀 Güncel Özellikler

- GitHub Webhook ile otomatik tetikleme (`POST /webhook/github`)
- Roslyn tabanlı statik analiz (kural bazlı bulgular)
- GPT destekli özet ve öneriler (opsiyonel, anahtar yoksa devre dışı)
- Arka plan işleyici (queue + worker)
- Blazor Server UI (10 sn'de bir otomatik yenileme, no-cache)
 - Blazor Server UI (SignalR ile gerçek zamanlı; toast + sesli uyarı)
- Kalite skoru (0-100)

> Not: SignalR gerçek zamanlı bildirimler şu an devre dışı. UI periyodik olarak yeniler.

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

## ⚡ Hızlı Başlangıç (Docker)
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
  "GitHub": { "WebhookSecret": "<your-webhook-secret>", "ApiUrl": "https://api.github.com" }
}
```

UI, API adresini `ApiSettings__BaseUrl` ile alır (örn. Railway API URL'si). Detay özeti (Summary) HTML render edilir; backend sanitize eder.

## 📊 API Endpoints (Güncel)
- `POST /webhook/github`
- `GET /api/dashboard`
- `GET /api/analyses`
- `GET /api/analyses/{id}`
- `GET /health`
 - SignalR Hub: `/hubs/analysis` (NewPullRequest, AnalysisUpdated, AnalysisCompleted)

## 🎯 Skor & Risk
- Roslyn: %60 (Error -10, Warning -5, Info -1)
- GPT: %40 (Critical -15, High -10, Medium -5, Low -2)

Risk: High / Medium / Low (bulgu sayısına göre)

## 🚀 Production (Örnek)
- Railway: Git push → auto-deploy
- Docker: `docker build` + `docker run`

> Örnek değerler placeholder'dır. Gerçek anahtarları paylaşmayın.

-----

# 🔍 CodeVision - AI-powered Code Review Assistant

Automatically analyzes GitHub PRs; produces Roslyn/GPT insights; processes jobs in background; displays results in a Blazor UI.

## 🚀 Features (Current)
- GitHub webhook trigger (`POST /webhook/github`)
- Roslyn static analysis
- Optional GPT-based summary and suggestions
- Background queue + worker
- Blazor Server UI (auto-refresh every 10s, no-cache)
- Quality score (0-100)

> Note: SignalR real-time notifications are disabled for now. UI polls periodically.

## 🏗️ Architecture
```
GitHub → Webhook → Queue → Background Worker → PostgreSQL → API → Blazor UI
                               ↳ Roslyn / GPT
```

## ⚡ Quick Start (Docker)
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
  "GitHub": { "WebhookSecret": "<your-webhook-secret>", "ApiUrl": "https://api.github.com" }
}
```

## 📊 API Endpoints
- `POST /webhook/github`
- `GET /api/dashboard`
- `GET /api/analyses`
- `GET /api/analyses/{id}`
- `GET /health`

## 📝 Notes
- UI disables caching and adds a cache-buster on requests
- Auto-refresh every 10 seconds (paused when modal is open)
- Keep secrets in environment variables (do not commit)

## 📄 License
MIT
