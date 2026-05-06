# Feature: Automatic Invoice Numbering

## Opis biznesowy
Zapewnienie spójnej i nieprzerwanej sekwencji numeracji faktur wychodzących bez konieczności ręcznego wpisywania numeru przez użytkownika.

## Format numeru
`INV/YYYY/NNN`
- `INV` — stały prefiks.
- `YYYY` — rok wystawienia (pobierany z daty dokumentu).
- `NNN` — trzycyfrowy numer sekwencyjny w ramach danego roku (np. 001, 042).

## Algorytm generowania
1. Pobranie wszystkich faktur z bazy, których numer zaczyna się od `INV/{rok}/`.
2. Wyciągnięcie najwyższego istniejącego numeru (sekwencji).
3. Inkrementacja o 1.
4. Formatowanie do 3 znaków z wiodącymi zerami (D3).

## Zalety
- **Brak kolizji**: Unikalność numeru jest gwarantowana przez bazę danych i sprawdzana w handlerze.
- **Wygoda**: Użytkownik skupia się na danych merytorycznych, system dba o formalności.
- **Zgodność**: Numeracja jest zgodna z polskimi przepisami księgowymi dotyczącymi chronologii.
