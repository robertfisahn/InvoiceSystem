# Zasady Obsługi Plików i Uploadu (File Ingestion Rules)

Zbiór reguł zapewniających bezpieczeństwo, prywatność i wydajność podczas pracy z plikami użytkowników.

## 🛡️ Bezpieczeństwo (Security First)
- **Private Storage**: NIGDY nie przechowuj plików biznesowych w `wwwroot`. Pliki muszą znajdować się poza drzewem publicznym (np. `App_Data/Storage`).
- **Authorization Check**: Każdy dostęp do pliku musi przechodzić przez akcję kontrolera, która sprawdza uprawnienia użytkownika przed wysłaniem pliku (`FileResult`).
- **Validation Pipeline**: Każdy upload musi przejść walidację:
    - Rozmiar: Max 10MB (lub konfigurowalny limit).
    - Rozszerzenie: Tylko biała lista (np. `.pdf`, `.jpg`, `.png`).
    - MIME Type: Sprawdzenie nagłówka pliku.
    - Magic Numbers: Weryfikacja sygnatury pliku (ochrona przed virus.exe o nazwie virus.pdf).

## 🚀 Wydajność i UX
- **Deduplikacja (Hashing)**: Przed zapisem i procesowaniem wygeneruj hash SHA-256 pliku. Pozwala to na wykrycie duplikatów i oszczędność miejsca/mocy AI.
- **Statusy Przetwarzania**: Używaj stanów dla długich procesów: `Pending`, `Processing`, `Completed`, `Failed`.
- **Background Tasks**: Długotrwałe operacje na plikach (np. kompresja, OCR) wykonuj asynchronicznie w tle.

## 📂 Przechowywanie (Storage)
- **Safe Filenames**: Nigdy nie używaj oryginalnej nazwy pliku na dysku. Generuj unikalne nazwy (np. GUID) i mapuj je w bazie danych.
- **Hierarchical Storage**: Przy dużej ilości plików stosuj strukturę folderów opartą na datach (np. `2026/05/14/file.pdf`), aby uniknąć limitów systemów plików.
