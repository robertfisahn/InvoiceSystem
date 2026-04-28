---
trigger: always_on
---

# Git Workflow — Zasady kontroli wersji

## Filozofia commitów

Jeden commit = jedna logiczna zmiana.
Commit ma opisywać CO i DLACZEGO — nie kiedy.

---

## Format wiadomości commita

```
<typ>: <krótki opis w czasie teraźniejszym>

<opcjonalny opis szczegółowy>
```

### Typy:

| Typ | Kiedy używać |
|-----|-------------|
| `feat:` | Nowy ficzer (np. `feat: GetInvoiceList`) |
| `fix:` | Naprawa błędu |
| `chore:` | Porządki, usuwanie plików, zmiany konfiguracji |
| `refactor:` | Zmiana kodu bez zmiany zachowania |
| `docs:` | Zmiany w dokumentacji, README |
| `style:` | Zmiany CSS/formatowanie bez logiki |

### Przykłady:

```bash
# ✅ Dobry commit
git commit -m "feat: GetInvoiceList — DataGrid z paginacją i wyszukiwaniem"
git commit -m "chore: remove local Bootstrap/jQuery (CDN instead)"
git commit -m "docs: add FRONTEND-ARCHITECTURE.md"
git commit -m "fix: _ViewStart moved to root — layout not loading in VSA"

# ❌ Złe commity
git commit -m "update"
git commit -m "fixes"
git commit -m "zmiany"
git commit -m "wip"
```

---

## Kiedy commitować

- Po **każdym ukończonym logicznym kroku** — nie na koniec dnia
- Ficzer gotowy (backend + frontend) → commit
- Porządki/usunięcie plików → osobny commit
- Poprawka buga → osobny commit

---

## Push — na oba remote

Projekt ma dwa remote: **GitHub** (`origin`) i **GitLab** (`gitlab`).
Zawsze pushuj na oba:

```bash
git push origin main; git push gitlab main
```

---

## Nie rób tego na main

- Nie rób `git push --force` na `main` jeśli commit już poszedł na remote
- Nie commituj plików `.db` — są w `.gitignore`
- Nie commituj `appsettings.json` z hasłami/kluczami API

---

## Przydatne komendy

```bash
git status                    # co jest zmienione
git diff                      # co dokładnie zmieniłem
git log --oneline -10         # ostatnie 10 commitów
git add -p                    # dodaj zmiany kawałek po kawałku (staged)
git push origin main; git push gitlab main   # push na oba
```
