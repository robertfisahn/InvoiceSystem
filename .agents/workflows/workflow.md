---
description: new-feature
---

# Workflow — Implementacja Feature Slice (VSA)

Postępuj zgodnie z tą instrukcją przy dodawaniu każdej nowej funkcjonalności. 
Ten dokument jest **bezwzględnie wiążący** dla AI.

**Wymagane czytanie przed pracą:**
- `architecture.md` (Struktura i zakazy)
- `coding-rules.md` (Standardy kodu i wydajność)
- `docs/Features/` (Opis konkretnego wymagania biznesowego)

---

## 🛑 Zasady Nienegocjowalne (Deterministic Rules)

1. **Output Contract (MANDATORY)**: Feature jest uznany za **NIEWAŻNY**, jeśli brakuje choć jednego z plików:
   - Controller, Command/Query, Handler, Validator, ViewModel, View (.cshtml).
2. **Single Iteration Rule**: Cały "plasterek" (od DB po UI) musi zostać wygenerowany w jednej iteracji. Brak "logiki na później".
3. **Output Format**: Każdy plik kodu musi być wygenerowany jako osobny, kompletny blok kodu (code-only, bez zbędnych komentarzy poza kodem).
4. **Feature Isolation**: Zakaz współdzielenia ViewModeli i bezpośrednich odwołań między różnymi feature'ami.
5. **No Shortcut Rule**: Zakaz stosowania `TODO`, `mocków` oraz "stubów". Kod musi być produkcyjny.
6. **Architectural Violation**: Jeśli AI musi złamać zasadę – **musi** najpierw poinformować użytkownika.

---

## Faza 1 — Planowanie i Kontrakt

1. **Analiza typu** — ustal czy to **Command** czy **Query**.
2. **Definicja ścieżki** — przygotuj folder: `src/Web/Features/{Moduł}/{Akcja}/`.
3. **Kontrakt** — ogłoś listę plików, które wygenerujesz (zgodnie z Output Contract).

## Faza 2 — Dane i Persystencja

1. **Model** — zmiany w encjach domenowych (`Domain/Entities/`).
2. **Konfiguracja** — zdefiniuj mapowania (np. Fluent API).
3. **Migracja** — wygeneruj instrukcje lub uruchom migrację zgodnie z technologią używaną w projekcie.

## Faza 3 — Logika Biznesowa (Application)

1. **Request** — stwórz `record` dla Command/Query.
2. **Validator** — stwórz `AbstractValidator<T>` dla Command/Query (nigdy dla ViewModel).
3. **ViewModel** — stwórz model silnie typowany dla widoku.
4. **Handler**:
   - Primary Constructor dla DI.
   - Pamiętaj o `CancellationToken`.
   - **Ręczne mapowanie** (encja → ViewModel) — bez AutoMappera.
   - W Query obowiązkowo **Projekcja (.Select)**.

## Faza 4 — Integracja Web i UI

1. **Controller** — cienki kontroler, jawny routing `[Route]`, brak `DbContext`.
2. **Widok** — plik `.cshtml` w folderze ficzera (zintegrowany z systemem UI projektu).

## Faza 5 — Weryfikacja (Checklist)

- [ ] Czy projekt się kompiluje?
- [ ] Czy użyto `record` i `Primary Constructor`?
- [ ] Czy w Query jest `.Select()` i brak `AsNoTracking` (projekcja to załatwia)?
- [ ] Czy brak `ViewBag`, `ViewData` i `DbContext` w kontrolerze?
- [ ] Czy walidacja dzieje się w pipeline MediatR (nie w handlerze)?
- [ ] Czy zachowano izolację (brak współdzielonych ViewModeli)?

---

> [!IMPORTANT]
> Zanim zgłosisz zakończenie zadania — sprawdź czy nie złamałeś żadnej zasady z sekcji **Forbidden** w `architecture.md`.
