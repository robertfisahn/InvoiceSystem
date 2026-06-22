# 🧾 InvoiceSystem — ASP.NET Core 9 Enterprise

Zaawansowany system do kompleksowego zarządzania fakturami, zbudowany w nowoczesnej architekturze **Modularnego Monolitu (Modular Monolith)** z zastosowaniem **Vertical Slice Architecture (VSA)** oraz **CQRS (MediatR)**. Aplikacja stanowi solidny fundament dla skalowalnych systemów klasy ERP/Back-office w ekosystemie .NET 9.

---

<details open>
<summary>🚀 <strong>Demo Wizualne — Jak to wygląda?</strong></summary>
<br>

#### 🔐 System Logowania (Modern Navy Style)
Stylowy, ciemny motyw z autorską walidacją i wysokim kontrastem.
![Login Screen](docs/img/login_preview.png)

#### 🧾 Lista Faktur (Application Shell)
Nowoczesny układ z bocznym paskiem nawigacji i tabelą danych w stylu Navy.
![Invoice List](docs/img/list_preview.png)

#### 📝 Wystawianie Faktury (Dynamic Forms)
Intuicyjny proces dodawania pozycji z automatycznym wyliczaniem kwot i walidacją.
![Create Invoice](docs/img/create_preview.png)

#### 📄 Podgląd Faktury (A4 Print Engine)
 Profesjonalny podgląd arkusza A4 z autorskim systemem pozycjonowania stopki.
![Invoice Details](docs/img/details_preview.png)

#### 🌗 System Motywów i UX
Inteligentne przełączanie trybów Dark/Light z pełną synchronizacją komponentów DevExtreme i autorskimi powiadomieniami "dymkowymi".

</details>

---

<details>
<summary>🛠️ <strong>Technologie</strong></summary>
<br>

- **Backend**: .NET 9 (C# 13), MediatR, FluentValidation, Entity Framework Core
- **Database**: SQLite (Development)
- **Frontend**: Razor

### 🎨 Modern UI & Frontend Architecture
Projekt przeszedł gruntowną modernizację warstwy prezentacji, stawiając na lekkość i profesjonalny wygląd:
- **Pure CSS Strategy**: Całkowite odejście od ciężkich bibliotek (DevExpress) na rzecz **Vanilla CSS + BEM + CSS Variables**.
- **A4 Print Engine**: Autorski system generowania dokumentów A4, zapewniający idealne pozycjonowanie stopki i podział stron.
- **Vertical Slice UI**: Style i skrypty są blisko logiki biznesowej, a reużywalne elementy (jak modale) są wydzielone do `Shared/Components`.
- **Navy Design System**: Nowoczesny, ciemny motyw z dbałością o kontrast i typografię (Inter).
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

<details open>
<summary>🏛️ <strong>Architektura Projektu</strong></summary>
<br>

Aplikacja została zaprojektowana w oparciu o cztery filary nowoczesnego inżynierii oprogramowania w .NET:

1. **Modular Monolith (Modularny Monolit):**
   Kod aplikacji podzielony jest na logiczne, niezależne i luźno powiązane moduły biznesowe zlokalizowane w folderze `Modules/` (np. `Invoices`, `Ksef`, `Contractors`, `Users`, `Dashboard`). Każdy moduł ma własny cykl życia, strukturę bazodanową i reguły biznesowe, co ułatwia ewentualne wydzielenie ich do osobnych mikroserwisów w przyszłości.
2. **Vertical Slice Architecture (VSA):**
   Wewnątrz każdego modułu kod zorganizowany jest wokół konkretnych funkcji biznesowych (ang. *features*), a nie warstw technicznych (Controllers, Services, Repositories). Każdy pionowy plaster zawiera wszystko, co jest potrzebne do obsłużenia danego żądania – od widoku Razor, przez model widoku, aż po logikę zapisu w bazie danych.
3. **CQRS za pomocą MediatR:**
   Rozdzielamy operacje zapisu (Commands) od operacji odczytu (Queries). Handlery żądań są silnie typowane, niezależne od siebie i łatwe w testowaniu jednostkowym.
4. **Clean Architecture (Elementy):**
   Logika domenowa (encje) jest odizolowana od zewnętrznych szczegółów technologicznych (np. bazy danych czy frameworka webowego).

#### 📁 Struktura folderów w module:
```text
Modules/[Moduł]/
├── Domain/           # Reguły biznesowe, encje domenowe
├── Infrastructure/   # Konfiguracja EF Core DbContext, integracje zewnętrzne (KsefClient itp.)
└── Features/         # Slices (Funkcjonalności pionowe)
    └── [NazwaFunkcji]/
        ├── [Funkcja]Command.cs             # Żądanie zapisu / Query (żądanie odczytu)
        ├── [Funkcja]CommandHandler.cs      # Logika biznesowa wykonująca żądanie
        ├── [Funkcja]ViewModel.cs           # Dane przesyłane do/z widoku
        ├── [Funkcja]Controller.cs          # Punkt wejścia (MVC Action)
        └── Index.cshtml                    # Warstwa prezentacji (Widok Razor)
```
</details>

---

<details>
<summary>📝 <strong>Zasady Pracy (Workflow)</strong></summary>
<br>

Projekt ściśle przestrzega następujących zasad technicznych:
1. **Module & Feature Isolation**: Zmiany w kodzie wprowadzamy zawsze wewnątrz dedykowanego modułu w katalogu `Modules/[Moduł]/Features/`. Unikamy współdzielenia kodu domenowego między modułami (komunikacja odbywa się przez MediatR / kontrakty).
2. **Primary Constructors**: Używane domyślnie dla wstrzykiwania zależności w klasach handlerów i kontrolerów.
3. **No AutoMapper**: Mapowanie typów encji na ViewModels/DTOs wykonujemy ręcznie w handlerach (lub za pomocą metod rozszerzających), co zapewnia pełną czytelność kodu.
4. **Git Workflow**: Jeden commit = jedna kompletna funkcjonalność biznesowa (Feature).

</details>

---
&copy; 2026 InvoiceSystem &mdash; Made with ☕ and .NET
