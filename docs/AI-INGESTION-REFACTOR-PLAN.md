# Plan Refaktoryzacji: Enterprise-Grade AI Ingestion System

Ten dokument opisuje proces transformacji prototypu importu faktur AI w bezpieczny, stabilny i skalowalny system klasy produkcyjnej.

## 📊 Obecny Stan (Techniczny Dług)
Obecna implementacja w `ImportInvoiceHandler` i `OllamaService` posiada krytyczne luki:
- **Security**: Pliki w publicznym `wwwroot` i brak walidacji typu MIME/Sygnatury.
- **Stability**: Brak timeoutów AI, brak walidacji JSON-a i kruche przekazywanie danych przez `TempData`.
- **Functionality**: Brak wsparcia dla skanów (brak warstwy tekstowej) oraz brak wykrywania duplikatów.

---

## 🗺️ Mapa Drogowa (Milestones)

### Faza 1: Security & Configuration (Krytyczne)
*Cel: Zabezpieczenie danych i usunięcie hardkodowanych ustawień.*
1.  **Private Storage**: Przeniesienie folderu `uploads` poza `wwwroot` (do `ContentRootPath`).
2.  **File Validation Service**: Implementacja rygorystycznej walidacji (MIME, sygnatura pliku, limit rozmiaru).
3.  **Strongly Typed Config**: Przeniesienie URL-i i modeli Ollamy do `appsettings.json` i `IOptions`.
4.  **Logging & Monitoring**: Dodanie `ILogger` i śledzenie czasu przetwarzania.

### Faza 2: Resilience & AI Robustness (Stabilność)
*Cel: Odporność na błędy AI i awarie modelu.*
1.  **Polly Resilience**: Dodanie timeoutów i polityki retry dla `HttpClient`.
2.  **Typed AI Response**: Zamiana `string` na `ExtractedInvoiceData?` z pełną walidacją schematu JSON.
3.  **Prompt Engineering**: Zabezpieczenie przed Prompt Injection i ustawienie `temperature: 0`.
4.  **Duplicate Detection**: Implementacja hashowania plików (SHA-256) przed wysłaniem do AI.

### Faza 3: Advanced Processing & OCR (Wydajność)
*Cel: Obsługa skanów i optymalizacja zasobów.*
1.  **OCR Pipeline**: Logika sprawdzająca, czy PDF ma tekst. Jeśli nie — fallback do OCR (np. model vision).
2.  **StringBuilder Optimization**: Poprawa wydajności przy długich dokumentach.
3.  **DTO Enrichment**: Dodanie brakujących pól (NIP, VAT breakdown, IBAN, waluta).

### Faza 4: Architecture Refactor (Skalowalność)
*Cel: Przejście na model asynchroniczny i modularny.*
1.  **Background Processing**: Wydzielenie analizy AI do `IBackgroundTaskQueue` lub `Worker Service` (aby nie blokować requestu HTTP).
2.  **Modularization**: Rozbicie handlera na mniejsze serwisy: `IFileStorage`, `IPdfExtractor`, `IAiExtractor`.
3.  **State Management**: Zamiana `TempData` na sesję bazy danych lub cache.

---

## 🛠️ Szczegółowa Analiza Zmian (Przykład)

| Funkcja | Obecnie (Źle) | Docelowo (Dobrze) | Dlaczego? |
|---------|---------------|-------------------|-----------|
| **Storage** | `wwwroot/uploads` | `App_Data/Storage` | Prywatność i RODO. Brak bezpośredniego dostępu przez URL. |
| **Parsing** | `rawText += text` | `StringBuilder` | Oszczędność pamięci RAM przy dużych plikach. |
| **Błędy** | `catch { return null; }` | `Log.Error(...)` | Możliwość diagnozowania problemów na produkcji. |
| **Model** | Hardcoded `llama3` | Configurable model | Elastyczność przy zmianie wersji modelu AI. |

---

## 📝 Kolejne kroki
Zaczynamy od **Fazy 1**, czyli zabezpieczenia plików i konfiguracji. To jest fundament, bez którego system jest niebezpieczny.
