const THEME_STATE_KEY = 'theme-mode';

$(function() {
    const savedTheme = localStorage.getItem(THEME_STATE_KEY) || 'dark';
    applyTheme(savedTheme);

    $('#themeToggle').on('click', function() {
        const currentTheme = $('html').attr('data-theme');
        const nextTheme = currentTheme === 'dark' ? 'light' : 'dark';
        
        applyTheme(nextTheme);
        localStorage.setItem(THEME_STATE_KEY, nextTheme);
    });
});

function applyTheme(theme) {
    $('html').attr('data-theme', theme);
    
    const icon = $('#themeIcon');
    if (theme === 'dark') {
        icon.removeClass('bi-moon').addClass('bi-brightness-high');
    } else {
        icon.removeClass('bi-brightness-high').addClass('bi-moon');
    }
}
