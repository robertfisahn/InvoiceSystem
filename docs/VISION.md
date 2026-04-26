# Vision — InvoiceSystem

## Co budujemy

System do automatycznego przetwarzania faktur oparty o AI.

Użytkownik wgrywa dokument (PDF, JPG, PNG) — system ekstrahuje tekst przez OCR,
parsuje dane przez agenta AI i zapisuje ustrukturyzowaną fakturę do bazy danych.
Wyniki są dostępne w dashboardzie z historią dokumentów.

---

## Cel projektu

Projekt portfolio pokazujący:
- głębokie zrozumienie ASP.NET Core MVC i Vertical Slice Architecture
- integrację z zewnętrznymi serwisami AI (Groq API, Ollama)
- przetwarzanie dokumentów (OCR, parsowanie)
- czystą architekturę, testowalny kod, CI/CD

---

## Stack technologiczny

- **Framework**: ASP.NET Core MVC (.NET 9)
- **Architektura**: Vertical Slice Architecture + CQRS + MediatR
- **UI**: Razor Views + DevExpress ASP.NET Core Controls (Enterprise UI)
- **Baza danych**: SQLite + EF Core (lekka, plikowa baza na start)
- **OCR**: do ustalenia (Tesseract lub zewnętrzne API)
- **AI**: Groq API (Llama 3.1) + Ollama (lokalnie)
- **Walidacja**: FluentValidation
- **Testy**: xUnit + FluentAssertions + NSubstitute

---

## Główne moduły

| Moduł | Opis |
|-------|------|
| `Invoices` | CRUD faktur, lista, szczegóły, usuwanie |
| `Upload` | Wgrywanie dokumentów PDF/JPG/PNG |
| `Ocr` | Ekstrakcja tekstu z dokumentu |
| `AiParser` | Parsowanie tekstu OCR przez agenta AI |
| `Dashboard` | Statystyki, ostatnie dokumenty, kolejka OCR |

---

## Przepływ przetwarzania faktury

```
Upload dokumentu (PDF/JPG/PNG)
↓
OCR — ekstrakcja tekstu
↓
AI Agent — parsowanie danych strukturalnych
(numer faktury, daty, pozycje, kwoty, kontrahent)
↓
Zapis do bazy danych
↓
Dashboard — widoczna faktura z danymi
```

---

## Zakres MVP

Na start budujemy tylko core flow:

1. Upload dokumentu
2. OCR — ekstrakcja tekstu
3. Zapis surowego tekstu
4. Ręczny podgląd i edycja faktury
5. Lista faktur

AI Parser, Dashboard i integracja Groq/Ollama — w kolejnym etapie.

---

## Czego NIE budujemy na razie

- Docker
- CI/CD
- Azure
- REST API / Swagger
- Autoryzacja / uwierzytelnianie
- Generator faktur testowych