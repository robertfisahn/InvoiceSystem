---
description: add-tests
---

# Testowanie — Strategia i Standardy

## Filozofia testowania

Kod ma być **testowalny by design** — nie dodajemy testów "na końcu", tylko piszemy kod który da się testować.

Osiągamy to przez:
- interfejsy dla serwisów zewnętrznych (mockowanie)
- DI wszędzie (łatwa podmiana zależności)
- cienkie Controllery (logika w Handlerach, Handlery łatwo testować)
- brak statycznych klas i `new` dla serwisów

---

## Dlaczego osobne projekty (.csproj)?

Unikamy trzymania wszystkich testów w jednym projekcie. Każdy typ testów = osobny `.csproj`.

1. **Dependency Pollution** — unit testy nie potrzebują `EF Core`, `TestContainers` ani `WebApplicationFactory`. Rozdzielenie gwarantuje że unit testy pozostaną czyste i szybkie
2. **Configuration Chaos** — testy integracyjne wymagają `appsettings.Test.json` i połączenia z bazą. Trzymanie ich razem z unitami prowadzi do konfliktów
3. **Slow Build/Test Cycles** — unit testy działają w milisekundach, integracyjne w minutach. Osobne projekty pozwalają odpalać je niezależnie

---

## Struktura projektów testowych

```
tests/
  Tests.Unit/                  ← osobny .csproj
  Tests.Integration/           ← osobny .csproj
  Architecture/                ← osobny .csproj
  E2E/                         ← osobny .csproj
```

| Projekt | Zależności | Nie może zależeć od |
|---------|------------|---------------------|
| `Tests.Unit` | xUnit, FluentAssertions, NSubstitute | Infrastructure, EF Core, Web |
| `Tests.Integration` | Respawn, TestContainers, EF Core | Web |
| `Architecture` | ArchUnitNET lub NetArchTest | — |
| `E2E` | WebApplicationFactory, Playwright (opcjonalnie) | — |

---

## Biblioteki

| Biblioteka       | Cel                                           |
|------------------|-----------------------------------------------|
| `xUnit`          | Framework testowy                             |
| `FluentAssertions`| Czytelne asercje                              |
| `NSubstitute`    | Mockowanie interfejsów                        |
| `Respawn`        | Reset bazy danych między testami              |
| `WebApplicationFactory` | Testy E2E w pamięci                     |
| `ArchUnitNET`    | Testy architektury                            |

---

## Unit Testy

Testują pojedynczy Handler lub Validator **w izolacji** — bez bazy, bez HTTP.
Wszystkie zależności są mockowane przez `NSubstitute`.
Przykłady zostaną dodane na etapie implementacji pierwszego ficzera.

---

## Integration Testy

Testują Handler **z prawdziwą bazą danych**. 
Używamy `Respawn` do resetu bazy między testami.
Bazowa klasa integracyjna zapewnia spójne środowisko dla wszystkich testów tej warstwy.

---

## E2E Testy

Testują **cały stack** — od HTTP request przez Controller, MediatR, Handler, aż do bazy.
Wykorzystujemy `WebApplicationFactory`, aby uruchamiać aplikację w pamięci podczas testów.

---

## Testy Architektury

Pilnują zależności między warstwami **automatycznie w CI/CD**.
Weryfikują, czy np. `Domain` nie posiada referencji do `Infrastructure` oraz czy kontrolery nie korzystają bezpośrednio z `DbContext`.

---

## Zasady testowania

- Każdy Handler ma swój test unit i integration.
- Każdy Validator ma swój test unit.
- Testy architektury uruchamiane w CI/CD przy każdym Pull Requeście.
- Nazewnictwo testów: `{Metoda}_{Warunek}_Should{OczekiwanyWynik}`.
- Jeden test sprawdza jedną rzecz (Single Assertion Principle).
- Brak logiki biznesowej w testach — tylko Arrange / Act / Assert.
