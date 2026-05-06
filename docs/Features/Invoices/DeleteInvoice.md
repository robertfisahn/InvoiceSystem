# Feature: DeleteInvoice

## Opis biznesowy
Umożliwia całkowite usunięcie dokumentu faktury z systemu. Akcja jest nieodwracalna i wymaga wyraźnego potwierdzenia przez użytkownika.

## Logika techniczna (Backend)
- **Slice**: `Features/Invoices/DeleteInvoice`
- **Command**: `DeleteInvoiceCommand`
- **Handler**: `DeleteInvoiceCommandHandler`
- **Zachowanie**: 
  - Kaskadowe usuwanie pozycji faktury (`InvoiceItem`) przez EF Core.
  - Sprawdzenie istnienia dokumentu przed usunięciem.
  - Zwraca `DeleteInvoiceResult` z flagą sukcesu lub opisem błędu.

## Interfejs użytkownika (Frontend)
- **Komponent**: DevExtreme Dialog (`DevExpress.ui.dialog.custom`).
- **Lokalizacja**: Przyciski "Usuń" na liście faktur oraz w podglądzie szczegółów.
- **Zabezpieczenia**: 
  - Wyłączone przesuwanie (dragEnabled: false).
  - Przycisk "Tak" wyróżniony kolorem niebezpieczeństwa (danger).
  - CSRF Protection (ValidateAntiForgeryToken).

## Feedback
- Po udanym usunięciu system wyświetla **Toast Notification** (dymek) w prawym górnym rogu ekranu.
