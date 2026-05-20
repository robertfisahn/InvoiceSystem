const SIDEBAR_STATE_KEY = 'sidebar-collapsed';

$(function() {
    const isCollapsed = localStorage.getItem(SIDEBAR_STATE_KEY) === 'true';
    if (isCollapsed) {
        $('body').addClass('sidebar-collapsed');
        updateToggleIcon(true);
    }

    $('#sidebarToggle').on('click', function() {
        $('body').toggleClass('sidebar-collapsed');
        const nowCollapsed = $('body').hasClass('sidebar-collapsed');
        localStorage.setItem(SIDEBAR_STATE_KEY, nowCollapsed);
        updateToggleIcon(nowCollapsed);
    });

    // Obsługa menu mobilnego
    $('#mobileMenuToggle').on('click', function(e) {
        e.stopPropagation();
        $('body').toggleClass('sidebar-open');
    });

    // Zamknięcie menu mobilnego przy kliknięciu poza nim lub na link
    $(document).on('click', function(e) {
        if ($(window).width() < 768) {
            if (!$(e.target).closest('.app-sidebar').length && !$(e.target).closest('#mobileMenuToggle').length) {
                $('body').removeClass('sidebar-open');
            }
        }
    });
    
    $('.sidebar-menu a').on('click', function() {
        if ($(window).width() < 768) {
            $('body').removeClass('sidebar-open');
        }
    });
});

function updateToggleIcon(isCollapsed) {
    const icon = $('#sidebarToggleIcon');
    if (isCollapsed) {
        // Gdy zwinięty — strzałka w prawo (rozwiń)
        icon.removeClass('bi-chevron-left').addClass('bi-chevron-right');
    } else {
        // Gdy rozwinięty — strzałka w lewo (zwiń)
        icon.removeClass('bi-chevron-right').addClass('bi-chevron-left');
    }
}
