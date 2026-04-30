# Workflow — Implementacja Feature Slice (VSA)

Postępuj zgodnie z tą instrukcją przy dodawaniu każdej nowej funkcjonalności. 
Ten dokument jest **bezwzględnie wiążący** dla AI i ma priorytet nad intuicją.

**Wymagane czytanie przed pracą:**
- `architecture.md` (Struktura i zakazy)
- `coding-rules.md` (Standardy kodu i profesjonalizm)
- `docs/Features/` (Wymagania biznesowe)

---

## 🛑 Zasady Nienegocjowalne (Deterministic Rules)

1. **Output Contract (MANDATORY)**: Feature jest uznany za **NIEWAŻNY**, jeśli brakuje choć jednego z plików:
   - Controller, Command/Query, Handler, Validator, ViewModel, View (.cshtml).
2. **Single Iteration Rule**: Cały "plasterek" (od DB po UI) musi zostać wygenerowany w jednej iteracji.
3. **SRP & CQRS Enforcement**: Każdy `IRequest` (Command lub Query) **MUSI** mieć swoją własną, dedykowaną klasę handlera. Zabrania się łączenia wielu operacji w jednym handlerze.
4. **Output Format**: Każdy plik kodu musi być wygenerowany jako osobny, kompletny blok kodu.
5. **Feature Isolation**: Zakaz współdzielenia ViewModeli i bezpośrednich odwołań między różnymi feature'ami.
6. **No Shortcut Rule**: Zakaz stosowania `TODO`, `mocków` oraz "stubów".
7. **Security by Default**: Wszystkie ficzery (poza Loginem) są chronione globalnym filtrem. Zakaz dodawania `[AllowAnonymous]` bez polecenia.
8. **Architectural Violation**: Jeśli AI musi złamać zasadę – **musi** najpierw poinformować użytkownika.

---

## Faza 1 — Planowanie i Kontrakt

1. **Analiza operacji** — określ wszystkie Commandy i Query potrzebne w tym slice'ie.
2. **Definicja ścieżki** — przygotuj folder: `src/Web/Features/{Moduł}/{Akcja}/`.
3. **Kontrakt** — ogłoś listę plików, które wygenerujesz (zgodnie z Output Contract).

## Faza 2 — Dane i Persystencja

1. **Model** — zmiany w encjach domenowych (`Domain/Entities/`).
2. **Konfiguracja** — zdefiniuj mapowania (np. Fluent API).
3. **Migracja** — wygeneruj instrukcje lub uruchom migrację.

## Faza 3 — Logika Biznesowa (Application)

1. **Requesty** — stwórz osobny `record` dla każdego Command i Query.
2. **Walidatory** — stwórz `AbstractValidator<T>` dla każdego Command/Query, które przyjmuje dane od użytkownika.
3. **ViewModel** — stwórz model silnie typowany (widok dedykowany).
4. **Handlery**: 
   - **Osobna klasa dla każdego requestu**.
   - Primary Constructor dla DI.
   - Pamiętaj o `CancellationToken`.
   - **Ręczne mapowanie** (encja → ViewModel) — bez AutoMappera.
   - W Query obowiązkowo **Projekcja (.Select)**.

## Faza 4 — Integracja Web i UI

1. **Controller** — cienki kontroler, jawny routing `[Route]`.
2. **Widok** — plik `Index.cshtml` w folderze ficzera (zintegrowany z systemem UI).

## Faza 5 — Weryfikacja (Checklist)

- [ ] Czy każdy Request ma osobny Handler (SRP)?
- [ ] Czy projekt się kompiluje?
- [ ] Czy użyto `record` i `Primary Constructor`?
- [ ] Czy w Query jest `.Select()` i brak `AsNoTracking` (projekcja to załatwia)?
- [ ] Czy brak `ViewBag`, `ViewData` i `DbContext` w kontrolerze?
- [ ] Czy walidacja dzieje się w pipeline MediatR?

---

## Faza 6 — Dokumentacja (README)

1. **Screenshot** — Uruchom aplikację i zrób zrzut ekranu.
2. **Zapis** — Zapisz w `docs/images/{feature_name}.png`.
3. **README** — Zaktualizuj sekcję Demo i Tabelę Statusu.

---

> [!IMPORTANT]
> Zanim zgłosisz zakończenie zadania — sprawdź czy nie złamałeś żadnej zasady z sekcji **Forbidden** w `architecture.md`.
