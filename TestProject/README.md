# TestProject - CodeVision Test Cases

Bu proje CodeVision AI kod inceleme sistemini test etmek için oluşturulmuştur.

## Test Senaryoları

### 🔴 BadSecurityCode.cs - Güvenlik Açıkları ve Kötü Uygulamalar

Bu dosya kasıtlı olarak aşağıdaki sorunları içerir:

**Güvenlik Açıkları:**
- SQL Injection vulnerabilities
- Hardcoded passwords and secrets
- Information disclosure
- Code injection risks
- Path traversal vulnerabilities
- Weak authentication

**Kod Kalite Sorunları:**
- Deeply nested conditions
- Magic numbers
- Poor exception handling
- Memory/connection leaks
- Performance issues
- Method doing too many things

**Beklenen CodeVision Analizi:**
- 🔴 Risk Level: HIGH
- 🔢 Quality Score: 20-30 (very low)
- 🚨 Multiple security warnings
- 🔧 Refactoring suggestions
- 📋 Best practice recommendations

## Kullanım

Bu kod **asla production'da kullanılmamalıdır**. Yalnızca CodeVision test amaçlıdır.
