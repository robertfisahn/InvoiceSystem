// theme.js — logika dark/light mode

(function () {
    const STORAGE_KEY = 'invoice-theme';
    const DEFAULT_THEME = 'dark';

    function getTheme() {
        const saved = localStorage.getItem(STORAGE_KEY);
        if (saved) return saved;
        
        return DEFAULT_THEME;
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem(STORAGE_KEY, theme);

        // Aktualizuj ikonę przycisku jeśli istnieje
        const btn = document.getElementById('theme-toggle-btn');
        if (btn) {
            btn.setAttribute('title', theme === 'dark' ? 'Przełącz na jasny' : 'Przełącz na ciemny');
            btn.textContent = theme === 'dark' ? '☀️' : '🌙';
        }
    }

    function toggleTheme() {
        const current = document.documentElement.getAttribute('data-theme') ?? DEFAULT_THEME;
        applyTheme(current === 'dark' ? 'light' : 'dark');
    }

    // Zaaplikuj motyw natychmiast (przed renderowaniem) — zapobiega flashowi
    applyTheme(getTheme());

    // Eksportuj na obiekt window żeby przycisk mógł wywołać
    window.toggleTheme = toggleTheme;
})();
