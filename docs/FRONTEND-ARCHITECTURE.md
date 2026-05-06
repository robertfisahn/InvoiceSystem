# Frontend Architecture — InvoiceSystem (Pure CSS Approach)

System wykorzystuje nowoczesną, lekką architekturę frontendową opartą na standardach **Vanilla CSS**, **CSS Custom Properties** oraz wzorcu **Application Shell**. Całkowicie wyeliminowano ciężkie biblioteki zewnętrzne (jak DevExtreme czy Tailwind) na rzecz pełnej kontroli nad kodem.

---

## 🏛 1. Application Shell Architecture

Aplikacja jest zbudowana wokół stabilnej powłoki, która nie zmienia się między przejściami stron. Tylko zawartość kontenera `<main>` jest dynamicznie wstrzykiwana przez serwer Razor.

- **`_Layout.cshtml`**: Główny szkielet definiujący strukturę HTML5 i importy.
- **`_Sidebar.cshtml`**: Boczna nawigacja z logicznym podziałem na moduły.
- **`_TopBar.cshtml`**: Górna belka akcji z dynamicznym wstrzykiwaniem przycisków przez `@RenderSection("PageActions")`.
- **`page-body`**: Główny kontener treści zoptymalizowany pod kątem czytelności i marginesów.

---

## 🎨 2. Design System & CSS Variables

Zamiast hardkodowanych kolorów, projekt wykorzystuje **Design Tokens** zdefiniowane w `wwwroot/css/variables.css`.

- **`--bg-primary`**: Podstawa ciemnego motywu Navy.
- **`--accent-primary`**: Główny kolor interakcji (fiolet indygo).
- **`--text-primary`**: Wysoki kontrast dla czytelności danych.
- **`--radius-md`**: Spójne zaokrąglenia (8px) dla wszystkich kart i przycisków.

Dzięki takiemu podejściu, zmiana kolorystyki całego systemu odbywa się w jednym pliku, bez konieczności edycji widoków.

---

## 🖨 3. A4 Print Engine (Smart Stretch)

Unikalną cechą projektu jest autorski silnik druku faktur, który rozwiązuje problemy natywnej paginacji przeglądarek.

### Mechanizm działania:
1.  **CSS Media Query (`@media print`)**: Ukrywa UI systemu (`.no-print`) i konfiguruje systemowe marginesy strony (`15mm`).
2.  **JavaScript Stretching**: Funkcja `printInvoice()` oblicza wysokość dokumentu i "rozciąga" go do wielokrotności strony A4 (264mm obszaru roboczego).
3.  **Flexbox Anchoring**: Wykorzystanie `margin-top: auto` sprawia, że stopka faktury jest zawsze przyklejona do dolnej krawędzi ostatniej strony wydruku.

---

## 🧩 4. Reusable Components (DRY)

Wspólne elementy UI są wydzielone do `Shared/Components/`, co pozwala na ich reużycie bez duplikacji kodu:
- **`_DeleteModal.cshtml`**: Centralna obsługa usuwania z bezpiecznym potwierdzeniem.
- **`_Badge.cshtml`**: Kolorowe statusy (Draft, Paid, Overdue) oparte o klasy BEM.
- **`_Alert.cshtml`**: Powiadomienia systemowe oparte o motyw Navy.

---

## 🛠 5. Standardy Kodowania UI
- **BEM (Block Element Modifier)**: Przejrzyste nazewnictwo klas (np. `.invoice-card__header--highlight`).
- **Stylizacja Hybrydowa**: 
  - Globalne style (layout, komponenty, zmienne) znajdują się w plikach `.css`.
  - Style unikalne dla ficzera (np. A4 Print Engine w detalach faktury) są umieszczone bezpośrednio w widoku Razor. Pozwala to na zachowanie zasad **Vertical Slice Architecture**, gdzie cała logika i prezentacja modułu są w jednym miejscu.
- **Dark-First**: System jest natywnie zaprojektowany w ciemnych barwach.
