---
trigger: always_on
---

# Zasady Kodowania — InvoiceSystem

## 🏢 Standard Komercyjny (MANDATORY)

Wszystkie projekty traktujemy jako systemy **komercyjne/enterprise**. 
1. **Zakaz używania zwrotów**: "portfolio project", "demo", "zadanie rekrutacyjne", "projekt do nauki".
2. **Jakość**: Kod, dokumentacja i UI muszą spełniać najwyższe standardy rynkowe.
3. **Poważne podejście**: Stopki, nazwy i opisy muszą brzmieć profesjonalnie (np. "System Zarządzania Fakturami", a nie "Moja Apka").

---

## Czystość Kodu i nazewnictwo

- **Kod** — wszystkie identyfikatory (klasy, metody, zmienne) i nazwy plików w języku **angielskim**
- **Routing** — adresy URL w języku angielskim (np. `/invoices`, `/invoices/create`)
- **Komentarze** — mogą być po polsku

---

## Nowoczesny C#

### Primary Constructors — używaj dla DI
Skrócona składnia wstrzykiwania zależności bezpośrednio w definicji klasy.

### Records dla Command i Query — są niezmienne
Używaj `record` dla wszystkich obiektów typu DTO, Command i Query.

### File-scoped namespaces — zawsze
Oszczędzaj miejsce i redukuj wcięcia stosując średnik po nazwie namespace.

---

## Controller

### Controller MA:
- Przyjmować request HTTP
- Wywołać `mediator.Send(command/query)`
- Zwrócić `View(viewModel)` lub `RedirectToAction()`
- Obsłużyć błędy walidacji (`ModelState`)

### Controller NIE MOŻE:
- Używać `DbContext` bezpośrednio
- Zawierać logiki biznesowej
- Wykonywać operacji I/O (pliki, zewnętrzne API)
- Mapować encji — mapowanie należy do Handlera

---

## CQRS — kiedy Command, kiedy Query

| Typ     | Kiedy używać                        | Zwraca                        |
|---------|-------------------------------------|-------------------------------|
| Command | Tworzenie, edycja, usuwanie danych  | `Unit` (void) lub nowe `Id`   |
| Query   | Odczyt, listowanie, dashboard       | ViewModel lub listę           |

Query **nigdy** nie modyfikuje stanu — tylko odczytuje.

---

## Handler

- Jeden Handler na jeden Command/Query
- Używaj Primary Constructor dla DI
- Ręczne mapowanie encja → ViewModel (bez AutoMappera)
- Zawsze przekazuj `CancellationToken`

---

## Walidacja — FluentValidation

- Jeden Validator na jeden Command/Query
- Walidacja uruchamiana automatycznie przez `ValidationBehavior` w pipeline MediatR
- Brak walidacji w Handlerze — od tego jest Validator

---

## EF Core — wydajność zapytań

### Projekcje `.Select()` — pobieraj tylko potrzebne dane
Zawsze stosuj projekcję do ViewModelu, aby uniknąć pobierania pełnych encji i narzutu Change Trackera.

### Paginacja — zawsze przy listach
Stosuj `Skip()` i `Take()` przy każdym zapytaniu zwracającym kolekcję danych.

### Konfiguracja encji
Każda encja ma osobny plik konfiguracji Fluent API w `Infrastructure/Persistence/Configurations/`.

---

## Asynchroniczność

- Zawsze `async/await` — brak synchronicznego kodu I/O
- **Każda metoda async przyjmuje i przekazuje `CancellationToken`**
- Używaj `Task.WhenAll` dla równoległych niezależnych operacji I/O

---

## ViewModel

- Osobny ViewModel na każdy feature — nigdy współdzielony
- Tylko właściwości potrzebne dla danego widoku
- Atrybuty `[Required]`, `[Display]`, `[MaxLength]` dla Razor form

---

## Razor View

- Zawsze `@model {Akcja}ViewModel` na górze pliku
- Brak logiki biznesowej w Razor
- Brak wywołań serwisów w Razor
- Proste `if` / `foreach` są dopuszczalne
- Formularze przez `asp-for`, `asp-action`, `asp-controller`

---

## Encje domenowe

- Konstruktor z wymaganymi parametrami — brak publicznych setterów gdzie możliwe
- Brak logiki infrastrukturalnej w Domain (EF, HTTP, pliki)
- Nullable reference types włączone

---

## Clean Code

- Brak magicznych stringów — używaj stałych lub `nameof()`
- Brak `new` dla serwisów i DbContextów — zawsze DI przez konstruktor
- **Fail Fast** — walidacja dzieje się na początku cyklu requestu (ValidationBehavior)

---

## Dependency Direction — kierunek zależności

Zależności płyną tylko w jednym kierunku:
- Web → Application → Domain
- Infrastructure → Application + Domain
- Domain → nic (zero zewnętrznych zależności)

Jeśli `Domain` importuje cokolwiek z EF Core lub HTTP — to błąd architektoniczny.

---

## Null Handling

- Nullable Reference Types zawsze włączone
- Unikaj operatora `!` (null-forgiving)
- Używaj jawnych null checków zamiast zakładać że coś nie jest null

---

## LINQ Rules

- Używaj `.Select()` zamiast `.Include()` gdy potrzebujesz tylko kilku pól
- Nigdy nie wywołuj `.ToList()` przed `.Select()`
- Nie wykonuj zapytań do bazy w pętli (N+1 problem)

---

## DTO vs ViewModel

Encja nigdy nie trafia do widoku. ViewModel nigdy nie trafia do bazy.

---

## Konfiguracja — IOptions\<T\>

Nie czytaj ustawień bezpośrednio z `IConfiguration` — używaj silnie typowanych klas opcji.
Każda sekcja konfiguracji ma swoją klasę `{Nazwa}Settings` w warstwie `Application` lub `Infrastructure`.

---

## Interfejsy dla serwisów zewnętrznych

Baza danych (EF Core) nie potrzebuje dodatkowej abstrakcji. Serwisy zewnętrzne **zawsze** mają interfejs zdefiniowany w warstwie Application.

---

## Konwencja nazewnictwa pól prywatnych

Przy **Primary Constructors**parametry są dostępne w całej klasie. Jeśli pole prywatne jest niezbędne, używaj prefiksu `_`.

---

## Zmienne środowiskowe — plik .env

Sekrety nigdy nie trafiają do repozytorium. Używamy pliku `.env` ładowanego przy starcie aplikacji.
Koniecznie dodaj `.env` do `.gitignore`.