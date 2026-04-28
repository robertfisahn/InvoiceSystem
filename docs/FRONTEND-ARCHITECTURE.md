# Frontend Architecture — InvoiceSystem

## Koncepcja: Application Shell + Feature-Based Views

Frontend działa jak **powłoka aplikacji (Application Shell)** — stabilny szkielet który nigdy się nie zmienia, plus zmienne widoki ficzerów wstrzykiwane w `<main>`.

---

## Diagram przepływu ładowania

```mermaid
graph TD
    Browser["🌐 Przeglądarka\nhttp://localhost:5215/invoices"]
    Browser --> Layout

    subgraph "Razor Engine (serwer)"
        Layout["_Layout.cshtml\n(Application Shell)"]
        Layout --> Navbar["Components/_Navbar.cshtml"]
        Layout --> Body["@RenderBody()"]
        Layout --> Footer["Components/_Footer.cshtml"]
        Body --> View["Features/Invoices/\nGetInvoiceList/Index.cshtml"]
    end

    subgraph "CSS — kolejność ładowania"
        V1["① frontend-variables.css\nDesign Tokens\n(kolory, spacing, radii)"]
        V2["② app.css\nGlobalne style\n(body, shell, utilities)"]
        V3["③ components/navbar.css"]
        V4["③ components/card.css"]
        V5["③ components/badge.css"]
        V6["③ components/table.css"]
        V1 --> V2
        V2 --> V3
        V2 --> V4
        V2 --> V5
        V2 --> V6
    end

    subgraph "JS — kolejność ładowania"
        J1["① theme.js\nWczytaj motyw z localStorage\n(PRZED renderem — brak flash)"]
        J2["② dx.all.js\nDevExtreme biblioteka"]
        J3["③ app.js\nAktywny link w nav\nGlobalne inicjalizacje"]
        J1 --> J2
        J2 --> J3
    end
```

---

## Hierarchia plików (nadrzędne → podrzędne)

```mermaid
graph LR
    subgraph "Warstwa 0 — Design Tokens"
        FV["frontend-variables.css\n⬛ Wszystkie zmienne CSS\n(--color-*, --spacing-*, --radius-*)"]
    end

    subgraph "Warstwa 1 — Global Shell"
        APP["app.css\nImportuje tokeny\nStyle dla body, .page, .app-shell"]
        LAYOUT["_Layout.cshtml\nHTML szkielet\nŁączy CSS + JS + komponenty"]
    end

    subgraph "Warstwa 2 — Komponenty CSS"
        NAV_CSS["components/navbar.css\n.navbar, .navbar__*, .theme-toggle"]
        CARD_CSS["components/card.css\n.data-card, .stat-card"]
        BADGE_CSS["components/badge.css\n.badge-status--*"]
        TABLE_CSS["components/table.css\n.dx-datagrid overrides\n.cell-invoice-number, .cell-amount"]
    end

    subgraph "Warstwa 2 — Komponenty Razor"
        NAVBAR["Shared/Components/_Navbar.cshtml\nHTML nawigacji\nUżywa klas z navbar.css"]
        FOOTER["Shared/Components/_Footer.cshtml\nHTML stopki"]
    end

    subgraph "Warstwa 3 — Widoki ficzerów"
        VIEW1["Features/Invoices/GetInvoiceList/\nIndex.cshtml\nUżywa: .page, .data-card,\n.cell-*, DevExtreme DataGrid"]
    end

    FV --> APP
    APP --> LAYOUT
    NAV_CSS --> NAVBAR
    LAYOUT --> NAVBAR
    LAYOUT --> FOOTER
    LAYOUT --> VIEW1
    CARD_CSS --> VIEW1
    TABLE_CSS --> VIEW1
```

---

## Jak działa przełączanie motywu

```mermaid
sequenceDiagram
    participant U as Użytkownik
    participant BTN as ☀️ Przycisk
    participant JS as theme.js
    participant HTML as &lt;html data-theme&gt;
    participant CSS as frontend-variables.css

    Note over JS: Przy ładowaniu strony
    JS->>JS: localStorage.getItem('invoice-theme')
    JS->>HTML: setAttribute('data-theme', 'dark')
    HTML->>CSS: [data-theme="dark"] aktywny
    CSS-->>U: Ciemne tło, jasny tekst

    Note over U: Klik w przycisk
    U->>BTN: onClick="toggleTheme()"
    BTN->>JS: window.toggleTheme()
    JS->>HTML: setAttribute('data-theme', 'light')
    JS->>JS: localStorage.setItem('invoice-theme', 'light')
    HTML->>CSS: [data-theme="light"] aktywny
    CSS-->>U: Jasne tło (#f3f4f6), ciemny tekst
```

---

## Struktura plików — pełna mapa

```
InvoiceSystem.Web/
│
├── _ViewStart.cshtml          ← Razor: domyślny layout dla wszystkich widoków
├── _ViewImports.cshtml        ← Razor: globalne using + tag helpers
│
├── Shared/
│   ├── _Layout.cshtml         ← APPLICATION SHELL (główny plik HTML)
│   └── Components/
│       ├── _Navbar.cshtml     ← Komponent nawigacji
│       └── _Footer.cshtml     ← Komponent stopki
│
├── Features/
│   └── Invoices/
│       └── GetInvoiceList/
│           └── Index.cshtml   ← Widok ficzera (wstrzykiwany w <main>)
│
└── wwwroot/
    ├── css/
    │   ├── frontend-variables.css  ← [1] Design Tokens (zawsze pierwszy)
    │   ├── app.css                 ← [2] Globalne style + shell
    │   └── components/
    │       ├── navbar.css          ← [3] Style nawigacji
    │       ├── card.css            ← [3] Style kart
    │       ├── badge.css           ← [3] Style statusów
    │       └── table.css           ← [3] Style tabeli + DevExtreme
    └── js/
        ├── theme.js               ← [1] Motyw (MUSI być przed renderem)
        └── app.js                 ← [2] Inicjalizacje UI
```

---

## Co jest nadrzędne, co podrzędne?

| Poziom | Plik | Rola |
|--------|------|------|
| **ROOT** | `frontend-variables.css` | Jedyne źródło prawdy dla kolorów i wartości |
| **SHELL** | `_Layout.cshtml` + `app.css` | Stabilny szkielet — nigdy się nie zmienia |
| **KOMPONENTY** | `_Navbar`, `_Footer`, `*.css` w `components/` | Reużywalne kawałki UI |
| **FEATURE** | `Features/{Moduł}/{Akcja}/Index.cshtml` | Zmieniana zawartość `<main>` per ficzer |

> **Zasada:** każdy nowy ficzer dodaje **tylko nowy widok** w `Features/`. Nigdy nie modyfikuje `_Layout.cshtml` ani plików shell.

---

## Dlaczego `site.css` i `site.js` są w projekcie?

To pozostałości po domyślnym szablonie ASP.NET — są **puste i nieużywane**. Można je usunąć lub zostawić bez wpływu na działanie.
