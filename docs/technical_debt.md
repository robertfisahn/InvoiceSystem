# Rejestr Długu Technicznego (Technical Debt Log) — InvoiceSystem

Ten plik służy jako **backlog refaktoryzacyjny** dla projektu. Za każdym razem, gdy Agent AI (lub programista) przegląda bazę kodu i natknie się na element niezgodny z oficjalnymi standardami opisanymi w dokumentacji (`docs/`), ma bezwzględny obowiązek dopisać go do poniższej listy jako "usterkę". 

Lista ta będzie sukcesywnie czyszczona w dedykowanych sesjach refaktoryzacyjnych.

---

## 🛑 Aktywne Odchylenia od Architektury (Do Poprawy)

### DT-005. Abstrakcja zapytań SQL specyficznych dla bazy danych (DbContext Extensions)
* **Status**: Otwarte (Planowane przy migracji na Modular Monolith)
* **Opis**: Zapytania SQL specyficzne dla silnika bazy danych (np. generowanie numeracji faktur w SQLite vs SQL Server) powinny być wydzielone z handlerów do metod rozszerzających `DbContext` w infrastrukturze modułu, aby zachować przenośność kodu.

### DT-006. Wyniesienie konfiguracji GUS BIR do .env oraz wsparcie HTTPS dla środowiska testowego
* **Status**: Otwarte (Do realizacji)
* **Opis**: Adres URL i klucz API integracji GUS SOAP są wpisane na sztywno. Należy przenieść je do klasy ustawień (`GusSettings`) wstrzykiwanej z `.env`. Ponadto, należy poprawić konfigurację `BasicHttpBinding` w C#, aby wspierała HTTPS (`BasicHttpSecurityMode.Transport`), co jest wymagane przez publiczne środowisko testowe GUS. Należy pamiętać o braku tagu `<StatusVat>` w prawdziwym API GUS (długoterminowo do zastąpienia API MF Białej Listy).

### DT-008. Architektura SaaS (Multi-tenancy) oraz skalowanie chmurowe
* **Status**: Otwarte (Architektoniczne / Do realizacji w przyszłości)
* **Opis**: Przygotowanie systemu pod wielu najemców (firmy). Wymaga wdrożenia izolacji danych za pomocą pola `TenantId` oraz mechanizmu Global Query Filters w EF Core. Należy również dostosować aplikację do skalowania poziomego (bezstanowość sesji w Redis, zapis plików w AWS S3 / Azure Blob Storage) oraz przenieść procesy integracji z KSeF na kolejki wiadomości (Message Queue, np. RabbitMQ/Hangfire/MassTransit) zamiast wykonywać je synchronicznie w kontrolerze MVC.



---

## ✅ Rozwiązane Odchylenia od Architektury (Historia)

### 1. Invoices — CreateInvoice (Niespójna struktura VSA)
* **Plik**: `Features/Invoices/CreateInvoice/GetCreateInvoiceQuery/GetCreateInvoiceQuery.cs`
* **Status**: Rozwiązane (31.05.2026)
* **Opis**: Rozbito na osobne, dedykowane pliki: `GetCreateInvoiceQuery.cs`, `GetCreateInvoiceHandler.cs` (jako `sealed class`) oraz `CreateInvoiceViewModel.cs` ( ViewModel + Lookup DTO).

### 2. Contractors — CreateContractor (Stary Command — Przed podziałem)
* **Plik**: `Features/Contractors/CreateContractor/CreateContractorCommand/CreateContractorCommand.cs`
* **Status**: Rozwiązane (31.05.2026)
* **Opis**: Rozbito na osobne, dedykowane pliki: `CreateContractorCommand.cs`, `CreateContractorCommandHandler.cs`, `CreateContractorValidator.cs` oraz `CreateContractorCommandController.cs`.

### 3. Invoices — GetInvoiceDetails (Niespójna struktura VSA)
* **Plik**: `Features/Invoices/GetInvoiceDetails/GetInvoiceDetailsQuery.cs`
* **Status**: Rozwiązane (31.05.2026)
* **Opis**: Rozbito na osobne, dedykowane pliki: `GetInvoiceDetailsQuery.cs`, `GetInvoiceDetailsHandler.cs` (jako `sealed class`) oraz `GetInvoiceDetailsViewModel.cs` (ViewModel + DTOs).

### 4. KSeF — Paczkowanie i Watermarking (DT-007)
* **Status**: Rozwiązane (26.06.2026)
* **Opis**: Wdrożono pobieranie w paczkach po maksymalnie 10 faktur, chronologiczne sortowanie i inkrementacyjny znak wodny (watermark) po każdej fakturze, zabezpieczając API przed statusem 429 i utratą danych.

### 5. KSeF — Współbieżność i blokada synchronizacji (DT-009)
* **Status**: Rozwiązane (26.06.2026)
* **Opis**: Wdrożono thread-safe lock `KsefSyncLock` koordynujący pobieranie ręczne i automatyczne w tle, co eliminuje nakładanie się sesji autoryzacji z jednego NIP-u.

### 6. Invoices — Walidacja kontrahenta przy tworzeniu faktury (DT-003)
* **Plik**: `Features/Invoices/CreateInvoice/CreateInvoiceCommand/CreateInvoiceHandler.cs`
* **Status**: Rozwiązane (28.06.2026)
* **Opis**: Wdrożono weryfikację istnienia kontrahenta w bazie danych przed zapisem i rzucanie wyjątku `InvalidOperationException` obsługiwanego w kontrolerze.

### 7. Globalna obsługa wyjątków w aplikacji (DT-004)
* **Status**: Rozwiązane (30.06.2026)
* **Opis**: Wdrożono centralny mechanizm przechwytywania błędów za pomocą `.NET 9 IExceptionHandler` (klasa `GlobalExceptionHandler`). Zwraca ustandaryzowany format *Problem Details* (JSON) dla zapytań API/KSeF oraz przyjazny widok HTML `/error` (ErrorsController + ErrorViewModel) z unikalnym Trace ID dla żądań tradycyjnych stron MVC.

### 8. Diagnostyka i monitoring wydajności (DT-010)
* **Status**: Rozwiązane (01.07.2026)
* **Opis**: Wdrożono strukturyzowane logowanie za pomocą biblioteki Serilog z dobowym podziałem plików (Logs/log-.txt) oraz zintegrowano potok MediatR z mechanizmem `PerformanceBehavior` automatycznie monitorującym czasy wykonania każdego handlera biznesowego przy użyciu `Stopwatch`. Powyżej progu 500 ms handler raportuje automatyczne ostrzeżenie o obciążeniu.
