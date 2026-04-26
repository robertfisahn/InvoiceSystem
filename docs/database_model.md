# Specyfikacja Modelu Bazy Danych — InvoiceSystem

Poniżej znajduje się docelowy model bazy danych, który stanowi fundament pod encje domenowe w systemie InvoiceSystem.

## Diagram ERD (Mermaid)

```mermaid
erDiagram
    CONTRACTOR ||--o{ INVOICE : creates
    INVOICE ||--o{ INVOICE_ITEM : contains
    INVOICE ||--o{ PAYMENT : has

    CONTRACTOR {
        bigint id PK
        string name
        string tax_id "NIP/REGON (nullable, unique)"
        text address "nullable"
        datetime created_at
        datetime updated_at
    }

    INVOICE {
        bigint id PK
        bigint contractor_id FK
        string invoice_number "unique"
        date date
        string file_path "nullable (path to PDF/scan)"
        datetime created_at
        datetime updated_at
    }

    INVOICE_ITEM {
        bigint id PK
        bigint invoice_id FK
        string name "Product/Service name"
        decimal quantity
        decimal unit_price
        decimal total_price
        datetime created_at
        datetime updated_at
    }

    PAYMENT {
        bigint id PK
        bigint invoice_id FK
        decimal amount
        string currency "default PLN"
        string method "przelew, gotówka, karta"
        date paid_at "nullable"
        datetime created_at
        datetime updated_at
    }
```

## Szczegóły pól i typów

### 1. Contractors (Kontrahenci)
Reprezentuje wystawcę lub odbiorcę faktury.
- `tax_id`: Kluczowy dla polskich faktur (NIP). Powinien być walidowany.
- `address`: Przechowywany jako tekst, w nowym systemie można rozważyć rozbicie na `Street`, `City`, `ZipCode`.

### 2. Invoices (Faktury)
Główny dokument w systemie.
- `invoice_number`: Unikalny identyfikator biznesowy (np. "FV/2024/04/001").
- `file_path`: Referencja do fizycznego pliku zapisanego na dysku/storagu.

### 3. InvoiceItems (Pozycje na fakturze)
- `total_price`: Kwota wyliczana jako `quantity * unit_price`, zapisywana bezpośrednio dla zachowania historycznej spójności danych finansowych.
- Typy `decimal(15,2)` są poprawne dla operacji finansowych (EF Core zmapuje je na odpowiedni format w SQLite).

### 4. Payments (Płatności)
Model wspiera wiele płatności do jednej faktury (płatności częściowe).
- `method`: Warto rozważyć zamianę na `Enum` (Przelew, Gotówka, Karta).
- `paid_at`: Data faktycznego wpływu środków.

---

## Proponowane usprawnienia dla nowego systemu (InvoiceSystem)

1.  **Value Objects**: Zastosowanie Value Objects dla `Money` (Amount + Currency) oraz `Address`.
2.  **Tax Rate**: Dodanie stopy podatku VAT (`TaxRate`) do pozycji faktury.
3.  **Invoice State**: Wprowadzenie statusu faktury (Draft, Issued, Paid, Overdue, Cancelled).
4.  **Contractor Types**: Rozróżnienie na `Vendor` (Sprzedawca) i `Customer` (Nabywca).
5.  **Audit Trail**: Śledzenie zmian (kto i kiedy zmodyfikował fakturę).
