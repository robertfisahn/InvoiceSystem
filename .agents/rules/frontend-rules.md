---
trigger: always_on
---

# Frontend Rules — Zasady CSS, HTML i Razor

## Zasady nadrzędne

- **Dark-first** — ciemny motyw jest domyślny, jasny jest opcjonalny
- **Bootstrap 5** — używamy komponentów Bootstrap, nadpisujemy przez CSS Variables
- **CSS Custom Properties** — zero hardkodowanych kolorów i wartości w CSS
- **BEM** — konwencja nazewnictwa klas dla własnych komponentów
- **Brak inline styles** — cały styl w plikach CSS
- **Brak `<style>` w Razor** — widoki nie zawierają stylów

---

## CSS Variables — jak używać

Nigdy nie hardkoduj kolorów ani wartości — zawsze używaj zmiennych z `variables.css`:

```css
/* ❌ Hardkodowane wartości */
color: #ffffff;
background: #1a1a2e;
padding: 16px;
border-radius: 8px;

/* ✅ CSS Variables */
color: var(--color-text-primary);
background: var(--color-bg-primary);
padding: var(--spacing-md);
border-radius: var(--radius-md);
```

---

## Dark / Light Theme

Motyw przełączany przez atrybut `data-theme` na elemencie `<html>`:

```html
<!-- Ciemny (domyślny) -->
<html data-theme="dark">

<!-- Jasny -->
<html data-theme="light">
```

Przełączanie przez JavaScript:

```javascript
// theme.js
const toggle = () => {
    const html = document.documentElement;
    const current = html.getAttribute('data-theme');
    const next = current === 'dark' ? 'light' : 'dark';
    html.setAttribute('data-theme', next);
    localStorage.setItem('theme', next);
};

// Zapamiętaj wybór użytkownika
const saved = localStorage.getItem('theme') ?? 'dark';
document.documentElement.setAttribute('data-theme', saved);
```

---

## BEM — nazewnictwo klas

Dla własnych komponentów (nie Bootstrap) używamy BEM:

```
.block {}                    ← komponent
.block__element {}           ← część komponentu
.block--modifier {}          ← wariant komponentu
.block__element--modifier {} ← wariant części
```

```css
/* Przykład — karta faktury */
.invoice-card { }
.invoice-card__header { }
.invoice-card__body { }
.invoice-card__footer { }
.invoice-card--highlighted { }
.invoice-card__status--paid { }
.invoice-card__status--overdue { }
```

---

## Bootstrap 5 — jak nadpisywać

Nadpisuj Bootstrap przez CSS Variables — nie przez `!important`:

```css
/* ❌ Brzydkie nadpisywanie */
.btn-primary {
    background-color: #your-color !important;
}

/* ✅ Nadpisywanie przez BS5 variables */
:root {
    --bs-primary: var(--color-primary);
    --bs-primary-rgb: var(--color-primary-rgb);
    --bs-body-bg: var(--color-bg-primary);
    --bs-body-color: var(--color-text-primary);
}
```

---

## Tabele danych

Tabele list (faktury, kontrahenci) zawsze:
- responsywne przez `<div class="table-responsive">`
- z hover przez `table-hover`
- z klasą `table-dark` w dark mode (przez CSS Variable)
- z paginacją przez komponent `_Pagination.cshtml`

```html
<div class="table-responsive">
    <table class="table table-hover align-middle">
        <thead>
            <tr>
                <th>Numer</th>
                <th>Kwota</th>
                <th>Status</th>
                <th></th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in Model.Items)
            {
                <tr>
                    <td>@item.Number</td>
                    <td>@item.Amount.ToString("C")</td>
                    <td>@await Html.PartialAsync("Components/_Badge", item.Status)</td>
                    <td>
                        <a asp-action="Details" asp-route-id="@item.Id"
                           class="btn btn-sm btn-outline-secondary">
                            Szczegóły
                        </a>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>
@await Html.PartialAsync("Components/_Pagination", Model.PageInfo)
```

---

## Formularze

Formularze zawsze:
- `asp-for` dla każdego pola — nigdy hardkodowane `name`
- komunikat błędu przez `asp-validation-for`
- przycisk submit z loading state (disabled po kliknięciu)

```html
<form asp-action="Create" method="post">
    <div class="mb-3">
        <label asp-for="Number" class="form-label"></label>
        <input asp-for="Number" class="form-control" />
        <span asp-validation-for="Number" class="text-danger small"></span>
    </div>

    <button type="submit" class="btn btn-primary"
            onclick="this.disabled=true; this.form.submit();">
        Zapisz
    </button>
</form>
```

---

## Upload Area

Drag & drop upload z wizualnym feedbackiem:

```html
<div class="upload-area" id="uploadArea">
    <div class="upload-area__icon">📄</div>
    <div class="upload-area__text">Przeciągnij plik lub kliknij</div>
    <div class="upload-area__hint">PDF, JPG, PNG — max 10MB</div>
    <input type="file" asp-for="File" class="upload-area__input" accept=".pdf,.jpg,.png" />
</div>
```

---

## Badge — statusy faktur

Statusy jako kolorowe badge — przez komponent `_Badge.cshtml`:

| Status | Kolor |
|--------|-------|
| Draft | `--color-status-draft` (szary) |
| Processing | `--color-status-processing` (niebieski) |
| Parsed | `--color-status-parsed` (fioletowy) |
| Confirmed | `--color-status-confirmed` (zielony) |
| Failed | `--color-status-failed` (czerwony) |

---

## Dashboard — karty statystyk

Karty zawsze z ikoną, liczbą i opisem:

```html
<div class="stat-card">
    <div class="stat-card__icon">🧾</div>
    <div class="stat-card__value">@Model.TotalInvoices</div>
    <div class="stat-card__label">Wszystkich faktur</div>
</div>
```

---

## Forbidden — czego nie rób

| Zakaz | Dlaczego |
|-------|----------|
| Inline `style=""` w HTML | Używaj klas CSS |
| `<style>` w plikach Razor | Styl należy do `wwwroot/css/` |
| Hardkodowane kolory w CSS | Używaj `var(--color-*)` |
| `!important` | Nadpisuj przez CSS Variables |
| Logika biznesowa w JS | JS tylko dla UI (theme, animacje) |
| Kopiowanie HTML między widokami | Wydziel do `Shared/Components/` |
