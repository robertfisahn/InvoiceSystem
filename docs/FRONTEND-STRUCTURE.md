# Frontend Structure — Application Shell

System wykorzystuje wzorzec **Application Shell Architecture**, który oddziela stałą strukturę nawigacyjną od dynamicznej treści poszczególnych modułów.

## 🏗 Układ (Layout Shell)

Struktura oparta jest na trzech głównych strefach zdefiniowanych w `_Layout.cshtml`:

1.  **TopBar (`_TopBar.cshtml`)**: Górna belka zawierająca profil użytkownika, wyszukiwarkę i przyciski akcji globalnych.
2.  **Sidebar (`_Sidebar.cshtml`)**: Boczna nawigacja z logicznym podziałem na sekcje (Dashboard, Dokumenty, Kontrahenci).
3.  **Main Content**: Dynamiczny obszar `<main>`, w którym renderowane są widoki z folderu `Features/`.

## 🎨 Design System (Pure CSS)

Zamiast polegać na zewnętrznych bibliotekach (Tailwind/DevExpress), system wykorzystuje autorskie podejście:
- **`variables.css`**: Wszystkie kolory, odstępy i promienie zaokrągleń (radius) są zdefiniowane jako **CSS Custom Properties**. Pozwala to na błyskawiczną zmianę motywu całego systemu.
- **Navy Theme**: Profesjonalna, ciemna paleta kolorystyczna zoptymalizowana pod kątem pracy w systemach enterprise.

## 🧩 Reużywalne Komponenty

Wspólne elementy UI znajdują się w `Shared/Components/`:
- `_DeleteModal.cshtml`: Centralny modal potwierdzający usuwanie.
- `_Pagination.cshtml`: Paginacja list danych.
- `_Badge.cshtml`: Statusy faktur (Draft, Paid, Overdue).

## 🖨 Print Engine (A4 Strategy)

Unikalną cechą układu jest dedykowany system druku:
- Automatyczne ukrywanie nawigacji (`.no-print`).
- Dynamiczne dociąganie stopki do dołu ostatniej strony A4 za pomocą skryptu `printInvoice()`.
- Systemowe marginesy `15mm` na każdej stronie.

## 🚀 Jak dodać nową stronę?

1.  Stwórz widok w `Features/{Moduł}/{Akcja}.cshtml`.
2.  Wykorzystaj `@section PageActions` dla przycisków w TopBarze.
3.  Treść główną owiń w `<div class="page-body">`.
