# Lista Feature-ów — InvoiceSystem

Poniżej znajduje się lista funkcjonalności systemu InvoiceSystem, zorganizowana zgodnie z architekturą **Vertical Slice Architecture (VSA)** w .NET. Każdy punkt reprezentuje pojedynczy "pionowy plaster" (Vertical Slice) do zaimplementowania.

## 1. Moduł: Invoices (Zarządzanie Fakturami)

Główny moduł obsługujący cykl życia dokumentu.

| Feature (Slice) | Opis | Typ (CQRS) |
|:--- |:--- |:--- |
| `GetInvoiceList` | Lista faktur w formie tabeli z paginacją i filtrowaniem. | Query |
| `GetInvoiceDetails` | Podgląd szczegółowy faktury (dane kontrahenta, pozycje, płatności). | Query |
| `CreateInvoice` | Ręczny formularz dodawania nowej faktury (wybór kontrahenta z listy). | Command |
| `UpdateInvoice` | Edycja danych faktury (zmiana daty, numeru, pozycji). | Command |
| `DeleteInvoice` | Usunięcie faktury z systemu wraz z powiązanymi pozycjami. | Command |
| `DownloadInvoiceFile` | Pobranie/wyświetlenie oryginalnego pliku dokumentu (PDF/obraz). | Query |

## 2. Moduł: OCR & AI (Automatyzacja Przetwarzania)

Inteligentne przetwarzanie dokumentów przy użyciu AI.

| Feature (Slice) | Opis | Typ (CQRS) |
|:--- |:--- |:--- |
| `UploadDocument` | Formularz uploadu pliku i wybór silnika AI (Groq / Ollama). | Command |
| `ProcessOcr` | Integracja z Tesseract (OCR) i wysłanie tekstu do LLM (Groq/Ollama). | Command |
| `ReviewOcrResults` | Widok prezentujący wyekstrahowany tekst i sformatowany JSON z AI. | Query |
| `CommitParsedInvoice` | Ostateczne zatwierdzenie danych z AI i stworzenie faktury w bazie. | Command |

## 3. Moduł: Contractors (Kontrahenci)

Zarządzanie bazą dostawców i odbiorców.

| Feature (Slice) | Opis | Typ (CQRS) |
|:--- |:--- |:--- |
| `GetContractorLookup` | Lista kontrahentów do wyboru w dropdownie (select). | Query |
| `CreateContractor` | Szybkie dodanie nowego kontrahenta (np. podczas OCR). | Command |

## 4. Moduł: Auth (Uwierzytelnianie)

Podstawowa kontrola dostępu.

| Feature (Slice) | Opis | Typ (CQRS) |
|:--- |:--- |:--- |
| `Login` | Formularz logowania i ustanowienie sesji. | Command |
| `Logout` | Zakończenie sesji użytkownika. | Command |

---

## Priorytety Implementacji (Zgodnie z VISION.md)

### Faza 1: Core CRUD (Fundament)
1. `GetInvoiceList`
2. `CreateInvoice` (Ręczne)
3. `GetInvoiceDetails`
4. `DownloadInvoiceFile`

### Faza 2: Infrastruktura OCR
1. `UploadDocument`
2. `ProcessOcr` (Implementacja Tesseract)
3. `ReviewOcrResults` (Prezentacja danych)

### Faza 3: Inteligencja AI
1. Integracja z Groq API (Llama 3.1)
2. Integracja lokalna z Ollama
3. `CommitParsedInvoice` (Zapis inteligentny)

### Faza 4: Polish & UX
1. Dashboard
2. Paginacja i wyszukiwanie
3. Statystyki wydatków
