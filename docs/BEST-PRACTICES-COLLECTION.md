# 📘 Kompendium Dobrych Praktyk — Enterprise Ingestion System

Ten dokument to zbiór zasad i standardów wypracowanych podczas budowy modułu AI Ingestion. Każdy punkt to rozwiązanie konkretnego problemu architektonicznego, bezpieczeństwa lub wydajności.

---

## 🛡️ KATEGORIA 1: Bezpieczeństwo i Pliki (File Security)

1.  **Private Storage Strategy** (Punkt 1): Pliki biznesowe (faktury) NIGDY nie mogą leżeć w `wwwroot`. Muszą być zapisywane poza dostępem publicznym (np. `App_Data/Storage`), aby uniknąć wycieku danych przez bezpośredni URL.
2.  **Multistage Upload Validation** (Punkt 2): Każdy plik musi przejść weryfikację:
    - Rozszerzenie (Whitelist).
    - Rozmiar (Limit np. 10MB).
    - Typ MIME (Czy PDF to faktycznie PDF?).
    - Sygnatura pliku (Magic Numbers — ochrona przed wirusem ukrytym pod nazwą `.pdf`).
3.  **Prompt Injection Protection** (Punkt 6): Tekst z faktury to dane zewnętrzne, nieufne. Należy oddzielać je od instrukcji systemowych (np. tagami XML/HTML) i stosować "System Prompts", aby AI nie wykonało poleceń zawartych w treści faktury.
4.  **Malware Scanning** (Punkt 18): Integracja z silnikiem antywirusowym (np. ClamAV lub Defender API) dla każdego przychodzącego pliku.
5.  **Authorization-Only Access**: Pobieranie plików tylko przez dedykowany endpoint kontrolera, który weryfikuje uprawnienia zalogowanego użytkownika.

---

## ⚙️ KATEGORIA 2: Integracja z AI (Ollama/LLM/OCR)

6.  **OCR Fallback Pipeline** (Punkt 3): System musi rozróżniać "Digital PDF" (z warstwą tekstową) od "Scanned PDF/Image" (puste). W przypadku braku tekstu, musi nastąpić automatyczny fallback do silnika OCR (Tesseract lub AI Vision).
7.  **Resilience & Timeouts** (Punkt 4): Połączenie z AI musi być odporne. Należy stosować bibliotekę Polly (Retry Policy, Exponential Backoff) oraz rygorystyczne timeouty (np. 30s), aby zawieszone AI nie "zabiło" serwera.
8.  **Strict JSON Validation** (Punkt 5): Wynik z AI (JSON) musi być traktowany jako "surowy tekst". Obowiązkowa deserializacja do modelu DTO i walidacja biznesowa (czy kwoty > 0, czy NIP poprawny).
9.  **Externalized AI Config** (Punkt 7): Adresy serwerów AI (Ollama URL), nazwy modeli i parametry (temperature, top_p) muszą być w `appsettings.json`, a nie w kodzie.
10. **Typed AI Service Contract** (Punkt 8): Serwis AI powinien zwracać silnie typowane obiekty `Task<ExtractedInvoiceData?>`, a nie surowe stringi, dla lepszej testowalności i IntelliSense.
11. **Confidence Scores** (Punkt 15): AI powinno (jeśli model pozwala) zwracać wskaźnik pewności dla każdego pola, co pozwala systemowi oznaczyć pola wymagające szczególnej uwagi człowieka.
12. **Determinism over Creativity**: Ustawianie `temperature: 0` dla zadań ekstrakcji danych, aby zminimalizować ryzyko halucynacji AI.

---

## ⚡ KATEGORIA 3: Wydajność i Skalowalność

13. **Memory Efficient Extraction** (Punkt 11): Przy zbieraniu tekstu z wielu stron dokumentu zawsze używaj `StringBuilder`. Unikaj `rawText += page.Text`, co generuje ogromny narzut na Garbage Collector.
14. **Asynchronous Background Processing** (Punkt 12): Przetwarzanie AI/OCR powyżej 2s musi dziać się w tle (Background Worker / Queue). Użytkownik nie może czekać z otwartym połączeniem HTTP na wynik długiego procesu.
15. **Duplicate Detection (Hashing)** (Punkt 17): Przed wysłaniem pliku do drogiego/wolnego AI, wylicz jego hash (SHA-256). Jeśli faktura o tym samym hashu już istnieje — odzyskaj dane z bazy/cache (Idempotency).
16. **API-First Architecture** (Punkt 19): Docelowo moduły Ingestion powinny być niezależnymi usługami API, co pozwala na ich łatwe skalowanie (np. oddzielny serwer tylko do OCR-a).

---

## 🛠️ KATEGORIA 4: Solidny Kod i UX

17. **Safe State Passing** (Punkt 13): Unikaj `TempData` do przesyłania dużych danych po OCR. Zapisuj wynik do sesji, bazy tymczasowej lub cache (Redis) i przekazuj tylko unikalny identyfikator procesu.
18. **Efficient DTOs** (Punkt 16): Model wyjściowy musi być precyzyjny. Zawieraj tylko te dane, które są niezbędne (np. NIP, kwoty, daty). Nie dodawaj pól "na zapas", których system nie będzie używał.
19. **Progress Status Management** (Punkt 14): Używaj stanów (Enum: `Processing`, `Extracted`, `Failed`, `Validated`), aby użytkownik zawsze wiedział, co dzieje się z jego dokumentem.
20. **Modular Pipeline** (Punkt 20): Podziel proces na niezależne serwisy: `IStorageService`, `IExtractorService`, `IAiAnalyzer`. Ułatwia to testy jednostkowe i wymianę modułów (np. przejście z Ollamy na Azure).
21. **Structured Logging & Monitoring** (Punkty 9, 10): Loguj każdy etap procesu. Dodaj metryki: czas trwania AI, liczba tokenów, success rate ekstrakcji.

---

*Ten dokument powinien być aktualizowany przy każdej zmianie architektonicznej. Służy jako standard jakości dla zespołu.*
