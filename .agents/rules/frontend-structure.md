---
trigger: always_on
---

# Frontend Structure — Application Shell + Feature-Based

## Koncepcja — Application Shell

UI zbudowany wokół stabilnej powłoki aplikacji.
Tylko zawartość `<main>` zmienia się między widokami — header i footer są zawsze obecne.

```
BODY
 ├─ HEADER   (statyczny — logo, nawigacja, theme toggle)
 ├─ MAIN     (dynamiczny — tu renderują się featureы)
 └─ FOOTER   (statyczny — copyright, linki)
```

To jest dokładnie ten sam mental model co VSA w backendzie:
- `_Layout.cshtml` = Application Shell
- `Features/{Moduł}/{Akcja}/{Akcja}.cshtml` = zawartość `<main>`

---

## Struktura plików frontend

```
src/Web/
  wwwroot/
    css/
      variables.css        ← tokeny designu (kolory, spacing, dark/light)
      app.css              ← globalne style, layout shell
      components/
        table.css          ← style tabel
        card.css           ← style kart
        form.css           ← style formularzy
        badge.css          ← style statusów/badge
        upload.css         ← style upload area
    js/
      app.js               ← inicjalizacja (theme toggle, Bootstrap)
      theme.js             ← logika dark/light mode
    img/
      logo.svg

  Shared/
    _Layout.cshtml         ← Application Shell (html, head, header, main, footer)
    _ViewImports.cshtml
    _ViewStart.cshtml
    Components/
      _Navbar.cshtml       ← nawigacja
      _ThemeToggle.cshtml  ← przełącznik dark/light
      _Footer.cshtml       ← stopka
      _Alert.cshtml        ← komunikaty sukces/błąd
      _Badge.cshtml        ← status faktury (Draft, Paid itd.)
      _Pagination.cshtml   ← paginacja list
      _Card.cshtml         ← karta z danymi

  Features/
    Invoices/
      List/
        List.cshtml        ← tabela faktur
      Details/
        Details.cshtml     ← szczegóły faktury
      Create/
        Create.cshtml      ← formularz tworzenia
      Upload/
        Upload.cshtml      ← drag & drop upload
    Dashboard/
      Index/
        Index.cshtml       ← karty ze statystykami
```

---

## Application Shell — _Layout.cshtml

Shell ma trzy strefy:

| Strefa | Odpowiedzialność |
|--------|-----------------|
| `<header>` | Logo, nawigacja, theme toggle — nigdy się nie zmienia |
| `<main>` | Tu wstrzykiwany jest `@RenderBody()` — zmienia się per feature |
| `<footer>` | Copyright, linki — nigdy się nie zmienia |

---

## Komponenty Shared

Komponenty to reużywalne kawałki UI wywoływane przez `@await Html.PartialAsync()`.

```csharp
// Przykład użycia w widoku
@await Html.PartialAsync("Components/_Badge", Model.Status)
@await Html.PartialAsync("Components/_Alert", TempData["Message"])
@await Html.PartialAsync("Components/_Pagination", Model.PageInfo)
```

Komponenty NIE zawierają logiki biznesowej — tylko renderują dane które dostają.

---

## Zasady struktury

- Jeden plik CSS per typ komponentu — nie jeden gigantyczny plik
- `variables.css` importowany jako pierwszy — przed wszystkim innym
- `app.css` importuje komponenty przez `@import`
- Brak inline styles w Razor — cały styl w plikach CSS
- Brak `<style>` tagów w widokach Razor
