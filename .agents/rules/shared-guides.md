---
trigger: always_on
---

# SHARED_GUIDES — Baza wiedzy wielokrotnego użytku

## Co to jest SHARED_GUIDES?

Folder `C:\Users\rober\.gemini\antigravity\SHARED_GUIDES` to baza wzorców i przewodników technicznych, które mogą być wykorzystane w wielu projektach.

Nie jest to dokumentacja konkretnego projektu — to **szablony podejść** do powtarzających się problemów.

---

## Kiedy tworzyć nowy SHARED_GUIDE?

Utwórz nowy plik `.md` w SHARED_GUIDES gdy:

- Implementujesz coś co **może się powtórzyć** w innym projekcie (auth, docker, CI/CD, paginacja)
- Rozwiązujesz problem który zajął dużo czasu — żeby nie tracić czasu drugi raz
- Wypracowujesz **decyzję architektoniczną** (np. Cookie vs JWT, LocalFiles vs CDN)
- Opisujesz wzorzec który ma konkretny szablon kodu do skopiowania

---

## Istniejące przewodniki

| Plik | Temat |
|------|-------|
| `ASPNET_AUTH_GUIDE.md` | Cookie Auth vs JWT, implementacja logowania w MVC |
| `DATABASE_GUIDE.md` | Wzorce pracy z bazą danych |
| `MIDDLEWARE_GUIDE.md` | Middleware w ASP.NET Core |
| `VALIDATION_GUIDE.md` | FluentValidation wzorce |
| `DOCKER_LARAVEL_ENV_GUIDE.md` | Docker + Laravel środowisko |
| `WEBAPP_ARCHITECTURE_GUIDE.md` | Architektura aplikacji webowych |
| `TESTING_STRATEGY_GUIDE.md` | Strategie testowania |
| `SIGNALR_ROADMAP.md` | SignalR implementacja |

---

## Format pliku SHARED_GUIDE

Każdy plik powinien zawierać:

1. **Kontekst** — kiedy używać tego podejścia
2. **Porównanie** — jakie są alternatywy i dlaczego wybraliśmy tę
3. **Implementacja krok po kroku** — gotowy kod do skopiowania
4. **Struktura plików** — gdzie co trafia w projekcie
5. **Checklist bezpieczeństwa / jakości** — co sprawdzić przed commitem

---

## Zasada: Sprawdź SHARED_GUIDES przed implementacją

Zanim zaczniesz implementować nowy ficzer lub rozwiązywać problem, sprawdź czy nie ma już gotowego przewodnika:

```
1. Czy temat jest w tabeli powyżej?
2. Jeśli tak — przeczytaj przewodnik PRZED pisaniem kodu
3. Jeśli nie — po implementacji utwórz nowy przewodnik
```
