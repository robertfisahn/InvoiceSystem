# 🧾 InvoiceSystem — ASP.NET Core 9 Enterprise

Zaawansowany system do kompleksowego zarządzania fakturami, zbudowany w architekturze **Vertical Slice Architecture (VSA)**. Aplikacja stanowi solidny fundament dla systemów klasy ERP/Back-office w ekosystemie .NET 9.

🏗️ [Architektura Projektu](docs/FRONTEND-ARCHITECTURE.md) | 📘 [Shared Guides](SHARED_GUIDES/ASPNET_AUTH_GUIDE.md)

---

<details open>
<summary>🚀 <strong>Demo Wizualne — Jak to wygląda?</strong></summary>
<br>

#### 🔐 System Logowania (ASP.NET Core Identity)
Stylowy, ciemny motyw z pełną walidacją po stronie serwera.
![Login Screen](docs/images/login_screen.png)

#### 📊 Lista Faktur
Interaktywna tabela (DevExtreme) z filtrowaniem, sortowaniem i paginacją.
![Invoice List](docs/images/invoice_list.png)

#### 📝 Wystawianie Faktury
Dynamiczny formularz z automatycznym wyliczaniem kwot i walidacją.
![Create Invoice](docs/images/create_invoice.png)

#### 📄 Podgląd Faktury (Paper Preview)
Profesjonalny widok dokumentu zoptymalizowany do druku (ukrywanie interfejsu systemowego).
![Invoice Details](docs/images/invoice_details.png)

</details>

---

<details>
<summary>🛠️ <strong>Technologie</strong></summary>
<br>

- **Backend**: .NET 9 (C# 13), MediatR, FluentValidation, Entity Framework Core
- **Database**: SQLite (Development)
- **Frontend**: Razor Views, Bootstrap 5, DevExtreme, CSS Variables (Theme Support)
- **Architektura**: Vertical Slice Architecture (VSA) — ficzery odseparowane od siebie.
- **Auth**: ASP.NET Core Identity (Cookie-based)

</details>

---

<details open>
<summary>⚙️ <strong>Szybki Start</strong></summary>
<br>

**Wymagania:** .NET 9 SDK

1. Sklonuj repozytorium:
```bash
git clone <url>
cd InvoiceSystem
```

2. Uruchom aplikację:
```bash
dotnet run --project InvoiceSystem.Web
```

✅ Aplikacja dostępna pod: **http://localhost:5215**
🔑 Dane logowania: `admin` / `Admin123!`

> **Baza danych:** SQLite (`InvoiceSystem.db`) tworzy się automatycznie przy starcie wraz z danymi testowymi (Seeding).

</details>

---

<details>
<summary>📋 <strong>Status Funkcjonalności</strong></summary>
<br>

| Funkcjonalność | Status | Opis |
| :--- | :---: | :--- |
| **Auth (Identity)** | ✅ | Login, Logout, Globalna ochrona |
| **Lista Faktur** | ✅ | Grid z danymi, filtrowanie |
| **Nowa Faktura** | ✅ | Formularz, dynamiczne pozycje |
| **Detale Faktury** | ✅ | Podgląd dokumentu (Paper Preview) |
| **Eksport PDF** | ⏳ | Planowane |

</details>

---

<details>
<summary>📝 <strong>Zasady Pracy (VSA Workflow)</strong></summary>
<br>

Projekt ściśle przestrzega zasad:
1. **Feature-based folders**: Każdy folder w `Features/` to niezależny moduł (Controller + Handler + View).
2. **Primary Constructors**: Używane wszędzie tam, gdzie to możliwe.
3. **No AutoMapper**: Mapowanie ręczne w handlerach dla pełnej kontroli.
4. **Git Workflow**: 1 feature = 1 commit.

</details>

---
&copy; 2026 InvoiceSystem &mdash; Made with ☕ and .NET
