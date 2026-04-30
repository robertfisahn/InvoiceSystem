---
trigger: always_on
---

# Git Workflow — Zasady kontroli wersji

## Filozofia commitów

Jeden commit = jedna logiczna zmiana.

### Zasada VSA — backend i frontend razem

W Vertical Slice Architecture **jeden feature = jeden commit**.
Handler bez widoku to niekompletny feature — nie rozdzielamy.

```bash
# ✅ Jeden commit na cały feature (6 plików = norma)
feat: CreateInvoice — Command, Handler, Validator, ViewModel, Controller, Razor View

# ❌ Sztuczne rozdzielanie — nie rób tego
feat: CreateInvoice backend
feat: CreateInvoice frontend
```

Ile plików na jeden feature w VSA:
```
CreateInvoiceController.cs    1
CreateInvoiceCommand.cs       2
CreateInvoiceHandler.cs       3
CreateInvoiceValidator.cs     4
CreateInvoiceViewModel.cs     5
Create.cshtml                 6
```
Czasem 7-8 jeśli dochodzi migracja lub nowa encja — ale nigdy 30+.

### Co idzie osobno (nie razem z featurem)

```bash
# Nowa encja lub konfiguracja = osobny commit PRZED featurem
chore: add Invoice entity and DbContext configuration

# Migracja = osobny commit PRZED featurem
chore: add CreateInvoices migration

# Porządki, usuwanie plików = osobny commit
chore: remove unused lib/ folder, switch Bootstrap to CDN

# Dokumentacja = osobny commit
docs: add FRONTEND-ARCHITECTURE.md
```

---

## Format wiadomości commita

```
<typ>: <krótki opis w czasie teraźniejszym>

<opcjonalny opis szczegółowy>
```

### Typy:

| Typ | Kiedy używać |
|-----|-------------|
| `feat:` | Nowy ficzer (np. `feat: GetInvoiceList`) — dodaje nową wartość dla użytkownika |
| `fix:` | Naprawa błędu — coś nie działało i teraz działa |
| `refactor:` | Zmiana kodu bez zmiany zachowania (np. rozbicie handlera na dwa, zmiana nazwy zmiennych) |

> [!IMPORTANT]
> **Zasada Krytyczna**: Jeśli dodajesz nową funkcjonalność, użyj `feat:`. Jeśli tylko poprawiasz strukturę istniejącego kodu (nawet jeśli to nowy kod, ale już był w repo), **ZAWSZE** użyj `refactor:`. 
> Sprawdź wiadomość dwa razy przed `push`, ponieważ na GitLabie branch `main` jest chroniony i nie pozwala na poprawki historii (`--force`).

---

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
