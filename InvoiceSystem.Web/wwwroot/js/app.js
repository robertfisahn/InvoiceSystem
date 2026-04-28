// app.js — inicjalizacja aplikacji

// DOMContentLoaded — Bootstrap init i inne globalne akcje UI
document.addEventListener('DOMContentLoaded', function () {
    // Zaznacz aktywny link w nawigacji
    const currentPath = window.location.pathname;
    document.querySelectorAll('.navbar__link').forEach(link => {
        if (link.getAttribute('href') === currentPath) {
            link.classList.add('navbar__link--active');
        }
    });
});
