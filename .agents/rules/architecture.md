---
trigger: always_on
---

# Architektura — VSA + CQRS + Razor

## Wzorzec: Vertical Slice Architecture

Zamiast tradycyjnych folderów `Controllers/`, `Models/`, `Views/` używamy folderu `Features/`.
Każdy podfolder reprezentuje pojedynczą funkcjonalność biznesową — **pionową kromkę**.

Dzielimy kod pionowo:
- Kontroler, Commandy/Query, Handlery, Walidatory, ViewModele i Widok Razor znajdują się w jednym folderze ficzera.

---

## Struktura projektu

```
src/
  Web/
    Features/
      {Moduł}/
        {Akcja}/
    Shared/              
    Setup/               
    Program.cs

  Application/
    Common/
      Behaviors/         

  Domain/                
    Entities/
    Enums/
    Exceptions/

  Infrastructure/        
    Persistence/
      AppDbContext.cs
      Configurations/
    Services/

tests/
```

---

## Warstwy i odpowiedzialności

| Warstwa        | Odpowiedzialność                                          |
|----------------|-----------------------------------------------------------|
| Web            | Controllers, Razor Views, ViewModels, routing, Middleware |
| Application    | Handlery, walidatory, pipeline MediatR, Behaviors         |
| Domain         | Encje, reguły biznesowe, wyjątki domenowe                 |
| Infrastructure | EF Core, zewnętrzne API, pliki, e-mail, baza danych       |

---

## Granica: Features vs Infrastructure

- **`Features/`** — wszystko co zmienia się wraz z wymaganiami biznesowymi (logika, widoki, walidatory, ViewModels).
- **`Infrastructure/`** — "rury techniczne" (DbContext, Middleware, konfiguracja bibliotek), które nie zmieniają się przy dodawaniu nowych ficzerów.
- **`Setup/`** (wewnątrz Web) — konfiguracja specyficzna dla MVC/Razor (np. ViewLocationExpander).

---

## Widoki Razor w folderze Features

Domyślnie MVC szuka widoków w `/Views/{Controller}/{Action}.cshtml`. W tym projekcie widoki są w `Features/` obok pozostałych plików slice'a.
Wymaga to implementacji `IViewLocationExpander` i rejestracji w `Program.cs`.

---

## Routing — atrybuty na każdym Controllerze

W VSA unikamy globalnych tras. Każdy Controller jawnie definiuje swoją trasę za pomocą atrybutu `[Route]`. Zapobiega to konfliktom nazw przy wielu kontrolerach o podobnych akcjach.

---

## Pipeline MediatR — Behaviors

Logika poprzeczna (Cross-cutting concerns) dzieje się w `IPipelineBehavior`, nie w Handlerach.
Standardowe zachowania to logowanie (`LoggingBehavior`) oraz automatyczna walidacja (`ValidationBehavior`).

---

## EF Core

- `AppDbContext` w warstwie `Infrastructure/Persistence/`.
- Każda encja posiada własną klasę konfiguracji `IEntityTypeConfiguration<T>`.
- `DbContext` nigdy nie trafia do Controllera.
- `DbContext` jest wstrzykiwany przez DI wyłącznie do Handlerów.

---

## Strategia Mapowania — ręczne mapowanie

W projekcie stosujemy jawne mapowanie (Explicit Mapping) zamiast AutoMappera.
Zalety: łatwe debugowanie, wydajność (brak refleksji), bezpieczeństwo typów (wykrywanie błędów w czasie kompilacji).

---

## Feature Contract

Każdy feature **musi** zawierać komplet plików zdefiniowanych w `workflow.md`.
Każdy proces (Command/Query) musi mieć swój dedykowany Handler.

---

## Konwencja nazewnictwa

Zasady nazewnictwa Commandów, Query, Handlerów i ViewModeli są ściśle zdefiniowane w pliku `context.md`.

---

## Lifecycle requestu

System przetwarza żądanie w ustalonym cyklu: Controller -> Mediator -> Behaviors -> Handler -> DbContext -> Response -> Razor View.

---

## Forbidden — czego nigdy nie rób

| Zakaz | Dlaczego |
|-------|----------|
| `AutoMapper` | Używamy jawnego mapowania — explicit > magic |
| `DbContext` w Controllerze | Controller nie może znać infrastruktury |
| Logika biznesowa w Razor | Widok tylko renderuje dane |
| Folder `Services/` dla logiki | Logika należy do Handlerów w `Features/` |
| `ViewBag` / `ViewData` | Używamy strongly typed ViewModel |
| Fat Controller | Controller wywołuje tylko `_mediator.Send()` |
| Repository Pattern | Używamy `AppDbContext` bezpośrednio w Handlerach |
| Statyczne klasy `static Helper` | Używaj DI i dedykowanych serwisów |
| Współdzielone ViewModele | Każdy feature ma własny ViewModel |