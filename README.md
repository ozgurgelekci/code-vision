# 🔍 CodeVision - AI Destekli Kod İnceleme ve Refaktör Asistanı

GitHub üzerinde açılan Pull Request'leri otomatik tarayıp; kodu özetleyen, potansiyel hataları/bad-practices tespit eden, refaktör önerileri sunan, kod kalitesini puanlayan ve sonuçları gerçek-zamanlı olarak geliştiriciye ileten bir servis.

## 🚀 Özellikler

- **GitHub Webhook Entegrasyonu**: PR açıldığında/güncellendiğinde otomatik analiz
- **Roslyn Statik Analiz**: .NET kodu için kesin kural tabanlı bulgular
- **GPT-4 AI Analizi**: Diff özeti, refaktör önerileri ve insan-dostu açıklamalar
- **Gerçek Zamanlı Bildirimler**: SignalR ile canlı güncellemeler
- **Modern Web UI**: Blazor Server ile responsive dashboard
- **Background İşleme**: Kuyruk sistemi ile ölçeklenebilir analiz
- **Kalite Puanlama**: 0-100 arası otomatik kod kalitesi değerlendirmesi

## 🏗️ Mimari

```
GitHub PR Event → Webhook → Analysis Queue → Background Worker
                                              ↓
Roslyn Analysis ← Worker → GPT Analysis → SignalR Notifications
       ↓                      ↓               ↓
   SQLite DB ←  Results → API Endpoints → Blazor UI
```

## 📦 Projeler

- **CodeVision.API**: Web API + SignalR Hub
- **CodeVision.Core**: Domain modelleri ve interface'ler
- **CodeVision.Infrastructure**: Veri erişimi ve servis implementasyonları
- **CodeVision.UI**: Blazor Server web arayüzü

## ⚡ Hızlı Başlangıç

### 🐳 Docker Compose ile (Önerilen)

#### 1. Projeyi Klonlayın
```bash
git clone <repo-url>
cd code-vision
```

#### 2. Environment Variables Ayarlayın
```bash
# .env dosyasını oluşturun
cp env.example .env

# Gerekli API anahtarlarını ekleyin
nano .env  # veya favori editörünüz
```

**Railway Environment Variables:**
```bash
OpenAI__ApiKey=sk-your-openai-api-key-here
GitHub__WebhookSecret=codevision-webhook-2025
GitHub__Token=ghp_your-github-token-here
```

#### 3. Hızlı Başlatma
```bash
# Shell script ile (Linux/macOS)
./docker-run.sh start

# Veya doğrudan docker-compose ile
docker-compose up -d
```

#### 4. Servislere Erişin
- **🌐 Dashboard**: http://localhost:3001
- **🔧 API**: http://localhost:5001  
- **📖 API Docs**: http://localhost:5001/swagger
- **🗄️ PostgreSQL**: localhost:5433
- **🏪 Redis**: localhost:6379

### 🛠️ Manuel Kurulum (Development)

#### 1. Gereksinimler
- .NET 9.0 SDK
- PostgreSQL 16+
- Redis (opsiyonel)

#### 2. Veritabanı Hazırlığı
```bash
# PostgreSQL'de veritabanı oluşturun
createdb codevision_db

# Migration'ları çalıştırın
dotnet ef database update --project CodeVision.Infrastructure --startup-project CodeVision.API
```

#### 3. Uygulamayı Çalıştırın
```bash
# API'yi çalıştır
cd CodeVision.API
dotnet run

# UI'yi çalıştır (farklı terminal)
cd CodeVision.UI
dotnet run
```

## 🔧 Konfigürasyon

### OpenAI Ayarları
```json
{
  "OpenAI": {
    "ApiKey": "sk-...",
    "Model": "gpt-4",
    "MaxTokens": 4000,
    "Temperature": 0.3
  }
}
```

### GitHub Ayarları
```json
{
  "GitHub": {
    "WebhookSecret": "codevision-webhook-2025",
    "Token": "ghp_...",
    "ApiUrl": "https://api.github.com"
  }
}
```

### Feature Flags
```json
{
  "Features": {
    "EnableGptAnalysis": true,
    "EnableRealTimeNotifications": true,
    "EnableBackgroundProcessing": true,
    "MaxConcurrentAnalyses": 5
  }
}
```

## 🔒 GitHub Webhook Kurulumu

1. GitHub repository → Settings → Webhooks
2. **Payload URL**: `https://your-domain.com/webhook/github`
3. **Content type**: `application/json`
4. **Secret**: webhook secret'ınızı girin
5. **Events**: Pull requests seçin
6. **Active**: ✅

## 📊 API Endpoints

### Webhook
- `POST /webhook/github` - GitHub webhook receiver

### Pull Request Analizleri
- `GET /api/pr/{repo}/{prNumber}` - Analiz durumunu getir
- `GET /api/pr/{repo}/{prNumber}/results` - Detaylı analiz sonuçları
- `POST /api/pr/{repo}/{prNumber}/re-run` - Analizi yeniden çalıştır

### Dashboard
- `GET /api/dashboard` - Genel metrikler
- `GET /api/dashboard/analyses` - Son analizler
- `GET /api/dashboard/notifications` - Bildirimler

## 🎯 Kalite Puanı Hesaplama

- **Roslyn Bulguları**: %60 ağırlık
  - Error: -10 puan
  - Warning: -5 puan
  - Info: -1 puan

- **GPT Önerileri**: %40 ağırlık
  - Critical: -15 puan
  - High: -10 puan
  - Medium: -5 puan
  - Low: -2 puan

**Risk Seviyeleri**:
- 🔴 **High**: 5+ error VEYA 1+ error + 10+ warning
- 🟡 **Medium**: 1+ error VEYA 15+ warning
- 🟢 **Low**: Diğer durumlar

## 🔧 Geliştirme

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 / VS Code
- Git

### Build & Test
```bash
dotnet build
dotnet test
```

### Database Migration
```bash
dotnet ef migrations add <MigrationName> --project CodeVision.Infrastructure --startup-project CodeVision.API
dotnet ef database update --project CodeVision.Infrastructure --startup-project CodeVision.API
```

## 🐳 Docker Komutları

### Script Komutları (Linux/macOS)
```bash
./docker-run.sh start     # Servisleri başlat
./docker-run.sh stop      # Servisleri durdur  
./docker-run.sh status    # Durum göster
./docker-run.sh logs      # Logları göster
./docker-run.sh migrate   # Migration çalıştır
./docker-run.sh clean     # Temizle
```

### Manuel Docker Compose
```bash
# Servisleri başlat
docker-compose up -d

# Durumu kontrol et
docker-compose ps

# Logları izle
docker-compose logs -f

# Servisleri durdur
docker-compose down

# Migration çalıştır
docker-compose exec codevision-api dotnet ef database update
```

## 🚀 Production Deployment

### Docker Production
```bash
# Production profili ile başlat (nginx dahil)
docker-compose --profile production up -d

# Veya
make prod
```

### Environment Variables
```bash
ASPNETCORE_ENVIRONMENT=Production
CODEVISION_OPENAI_APIKEY=your-key
CODEVISION_GITHUB_WEBHOOKSECRET=your-secret
CODEVISION_GITHUB_TOKEN=your-token
CODEVISION_CONNECTIONSTRINGS_DEFAULTCONNECTION="your-db-connection"
```

## 📈 Monitoring & Logs

- **Application Logs**: Serilog ile yapılandırılmış
- **Health Checks**: `/health` endpoint'i
- **Metrics**: `/api/dashboard/status` sistem durumu

## 🤝 Katkıda Bulunma

1. Fork'layın
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Commit'leyin (`git commit -m 'Add amazing feature'`)
4. Push'layın (`git push origin feature/amazing-feature`)
5. Pull Request açın

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır. Detaylar için `LICENSE` dosyasına bakın.

## 🆘 Destek

- 📧 Email: [destek@codevision.com]
- 🐛 Issues: GitHub Issues
- 📖 Dokümantasyon: [Wiki](wiki-url)

## 🎉 Özel Teşekkürler

- OpenAI GPT-4 API
- Microsoft Roslyn
- ASP.NET Core & Blazor
- SignalR

---

**CodeVision** ile kod kalitesini bir sonraki seviyeye taşıyın! 🚀
