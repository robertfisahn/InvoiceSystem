# Specyfikacja Ollama (Ollama Implementation Rules)

Zasady specyficzne dla pracy z lokalnym serwerem Ollama.

## ⚙️ Konfiguracja Lokalna
- **Local Endpoint**: Domyślny adres `http://localhost:11434/api/generate`. Musi być konfigurowalny przez `appsettings.json`.
- **Model Selection**: Zawsze używaj nazw modeli zdefiniowanych zewnętrznie (np. `llama3`, `mistral`).
- **JSON Format Mode**: Zawsze przesyłaj parametr `"format": "json"` w ciele requestu, aby wymusić strukturalny wynik na poziomie silnika modelu.

## 🛠️ Specyfika Implementacji
- **HttpClient Persistence**: Używaj `HttpClient` zarejestrowanego przez DI (AddHttpClient), aby uniknąć problemów z gniazdami przy częstych zapytaniach.
- **Vision Models Handling**: Przy fakturach obrazkowych (PNG/JPG) używaj modeli multimodalnych (np. `llava`, `llama3-vision`).
- **Prompting Framework**: Używaj rygorystycznych instrukcji systemowych: "Jesteś ekspertem od analizy faktur. Zwróć WYŁĄCZNIE surowy JSON bez wstępu i zakończenia".

## 🛑 Ograniczenia
- **Resource Management**: Pamiętaj o dużym zużyciu RAM przez lokalne modele. Przy dużym obciążeniu (młyn w firmie) rozważ przeniesienie Ollamy na oddzielną maszynę z mocnym GPU.
