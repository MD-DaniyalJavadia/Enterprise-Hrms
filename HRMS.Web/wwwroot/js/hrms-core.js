(function ($) {
    'use strict';

    const THEME_KEY = 'hrms_theme';
    const SIDEBAR_KEY = 'hrms_sidebar_collapsed';

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        const icon = $('#themeIcon');
        if (theme === 'dark') {
            icon.removeClass('bi-moon-stars').addClass('bi-sun-fill');
        } else {
            icon.removeClass('bi-sun-fill').addClass('bi-moon-stars');
        }
    }

    function getCsrfToken() {
        return $('meta[name="csrf-token"]').attr('content') || '';
    }

    $(function () {
        // Restore theme
        const savedTheme = localStorage.getItem(THEME_KEY) || 'light';
        applyTheme(savedTheme);

        $('#themeToggleBtn').on('click', function () {
            const current = document.documentElement.getAttribute('data-bs-theme') || 'light';
            const next = current === 'dark' ? 'light' : 'dark';
            localStorage.setItem(THEME_KEY, next);
            applyTheme(next);
            $(document).trigger('hrms:themeChanged', [next]);
        });

        // Restore sidebar state on desktop
        if (window.innerWidth >= 992 && localStorage.getItem(SIDEBAR_KEY) === 'true') {
            $('#hrmsWrapper').addClass('sidebar-collapsed');
        }

        $('#sidebarToggleBtn').on('click', function () {
            const wrapper = $('#hrmsWrapper');
            if (window.innerWidth < 992) {
                wrapper.toggleClass('sidebar-mobile-open');
            } else {
                wrapper.toggleClass('sidebar-collapsed');
                localStorage.setItem(SIDEBAR_KEY, wrapper.hasClass('sidebar-collapsed'));
            }
        });

        $('#sidebarMobileClose').on('click', function () {
            $('#hrmsWrapper').removeClass('sidebar-mobile-open');
        });

        // Global AJAX setup with CSRF header and error handling
        $.ajaxSetup({
            headers: {
                'X-CSRF-TOKEN': getCsrfToken(),
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        $(document).ajaxError(function (_event, jqXHR) {
            if (jqXHR.status === 401) {
                window.location.href = '/Account/Login';
                return;
            }
            if (jqXHR.status === 403) {
                Swal.fire('Access Denied', 'You do not have permission to perform this action.', 'warning');
            }
        });

        // Mark single notification read
        $('#notificationDropdownList').on('click', 'a[data-notification-id]', function () {
            const id = $(this).data('notification-id');
            if (!id) return;
            $.post('/Dashboard/MarkNotificationRead', { notificationId: id });
        });

        // Mark all notifications read
        $('#markAllReadBtn').on('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            $.post('/Dashboard/MarkAllNotificationsRead').done(function (res) {
                if (res && res.success) {
                    $('#unreadNotificationBadge').remove();
                    $('#notificationDropdownList .list-group-item').removeClass('bg-primary-subtle bg-opacity-10');
                    $('#markAllReadBtn').remove();
                }
            });
        });

        // Global search debounce
        let searchTimer = null;
        const $searchInput = $('#globalSearchInput');
        const $searchResults = $('#globalSearchResults');

        $searchInput.on('input', function () {
            clearTimeout(searchTimer);
            const term = $(this).val().trim();
            if (term.length < 2) {
                $searchResults.addClass('d-none').empty();
                return;
            }

            searchTimer = setTimeout(function () {
                $.get('/Dashboard/GlobalSearch', { term: term }).done(function (res) {
                    $searchResults.empty();
                    const items = (res && res.data) ? res.data : [];
                    if (!items.length) {
                        $searchResults.append(
                            $('<div class="px-3 py-2 small text-body-secondary">').text('No matching records found.')
                        );
                    } else {
                        items.forEach(function (item) {
                            const $link = $('<a class="dropdown-item rounded-2 py-2 d-flex align-items-center gap-2">')
                                .attr('href', item.url || '#');
                            $link.append($('<i>').addClass('bi ' + (item.iconClass || 'bi-search') + ' text-primary'));
                            const $text = $('<div>');
                            $text.append($('<div class="fw-semibold small">').text(item.title));
                            $text.append($('<div class="text-body-secondary" style="font-size:0.75rem;">').text(item.category + ' • ' + item.subtitle));
                            $link.append($text);
                            $searchResults.append($link);
                        });
                    }
                    $searchResults.removeClass('d-none').addClass('show');
                });
            }, 260);
        });

        $(document).on('click', function (e) {
            if (!$(e.target).closest('.hrms-global-search').length) {
                $searchResults.addClass('d-none').removeClass('show');
            }
        });

        // Phased module navigation notice for Phase 2-14 links
        $('.hrms-phase-link').on('click', function (e) {
            e.preventDefault();
            const moduleName = $(this).data('module');
            const phase = $(this).data('phase');
            Swal.fire({
                icon: 'info',
                title: moduleName,
                text: 'Scheduled for implementation in ' + phase + ' of the enterprise rollout.',
                confirmButtonText: 'OK'
            });
        });
    });
})(jQuery);
