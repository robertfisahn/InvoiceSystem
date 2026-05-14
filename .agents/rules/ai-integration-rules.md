# Zasady Integracji z AI (AI Integration Rules)

Zbiór uniwersalnych zasad współpracy z modelami LLM w aplikacjach biznesowych.

## 🛡️ Stabilność i Niezawodność
- **Resilience Policy**: Każde połączenie z AI musi posiadać:
    - Timeout (np. 30s-60s).
    - Retry Policy (Polly) z wykładniczym czasem oczekiwania.
- **Typed Responses**: Zakaz używania surowych stringów z AI. Zawsze deserializuj wynik do modelu DTO i waliduj go biznesowo.
- **Fallbacks**: Zawsze planuj scenariusz, gdy AI zawiedzie (np. powrót do ręcznego wypełniania danych).

## 🧠 Inżynieria Promptów (Security & Control)
- **Prompt Isolation**: Oddzielaj dane użytkownika od instrukcji systemowych (np. używając tagów `<data>...</data>`).
- **Prompt Injection Protection**: Traktuj każdą treść wejściową (np. tekst faktury) jako potencjalne zagrożenie.
- **Deterministic Output**: Ustawiaj `temperature: 0` dla zadań ekstrakcji danych, aby zminimalizować "kreatywność" i halucynacje.

## 📊 Monitoring i Rozwój
- **Confidence Scoring**: Jeśli to możliwe, proś model o ocenę pewności wyodrębnionych danych.
- **Metrics**: Loguj czas trwania (TTFT - Time To First Token) oraz całkowity czas ekstrakcji.
