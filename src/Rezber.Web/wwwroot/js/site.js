$(function () {

    const themeToggle = document.querySelector('[data-theme-toggle]');
    if (themeToggle) {
        const themeIcons = themeToggle.querySelectorAll('[data-theme-icon]');

        function applyTheme(theme) {
            document.documentElement.dataset.theme = theme;
            document.documentElement.dataset.bsTheme = theme;
            const nextTheme = theme === 'light' ? 'dark' : 'light';
            themeToggle.setAttribute('aria-label', `Switch to ${nextTheme} theme`);
            themeToggle.setAttribute('title', `Switch to ${nextTheme} theme`);
            themeIcons.forEach(icon => {
                icon.hidden = icon.dataset.themeIcon !== nextTheme;
            });
        }

        applyTheme(document.documentElement.dataset.theme === 'light' ? 'light' : 'dark');
        themeToggle.addEventListener('click', () => {
            const nextTheme = document.documentElement.dataset.theme === 'light' ? 'dark' : 'light';
            applyTheme(nextTheme);
            try {
                localStorage.setItem('rezber-theme', nextTheme);
            } catch { }
        });
    }
    
    //  Global search → go to /Packages?q=...
    const $globalSearch = $('#globalSearchInput');
    if ($globalSearch.length) {
        $globalSearch.on('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                const q = $(this).val().trim();
                window.location.href = '/PackageBrowser' + (q ? '?q=' + encodeURIComponent(q) : '');
            }
        });
    }
    
    //  Filters (chip / target / license / toggles)    
    const $filterForm = $('#filterForm');
    if ($filterForm.length) {

        $filterForm.find('input[name="q"]').on('input', debounce(function () {
            submitFilterForm();
        }, 400));

        $filterForm.find('.chip').on('click', function () {
            $filterForm.find('.chip').removeClass('active');
            $(this).addClass('active');
            $filterForm.find('input[name="filter"]').val($(this).data('filter'));
            submitFilterForm();
        });

        $filterForm.find('.filter-chip[data-target]').on('click', function () {
            $filterForm.find('.filter-chip[data-target]').removeClass('active');
            $(this).addClass('active');
            $filterForm.find('input[name="target"]').val($(this).data('target'));
            submitFilterForm();
        });

        $filterForm.find('.filter-chip[data-license]').on('click', function () {
            $filterForm.find('.filter-chip[data-license]').removeClass('active');
            $(this).addClass('active');
            $filterForm.find('input[name="license"]').val($(this).data('license'));
            submitFilterForm();
        });

        $('#filterMine').on('change', function () {
            $filterForm.find('input[name="mine"]').val(this.checked ? 'true' : 'false');
            submitFilterForm();
        });

        $('#filterRecent').on('change', function () {
            $filterForm.find('input[name="recent"]').val(this.checked ? 'true' : 'false');
            submitFilterForm();
        });
    }

    function submitFilterForm() {
        if ($filterForm && $filterForm.length)
            $filterForm[0].submit();
    }

    function debounce(fn, wait) {
        let t;
        return function () {
            clearTimeout(t);
            const args = arguments;
            const ctx = this;
            t = setTimeout(() => fn.apply(ctx, args), wait);
        };
    }

    
    //  Copy to clipboard    
    $(document).on('click', '[data-copy]', function () {
        const $btn = $(this);
        const text = $btn.data('copy');
        if (!text) return;

        navigator.clipboard.writeText(text).then(() => {
            const originalHtml = $btn.html();
            $btn.addClass('copied').html('<i class="fas fa-check"></i> Copied');
            setTimeout(() => {
                $btn.removeClass('copied').html(originalHtml);
            }, 1500);
        }).catch(() => {
            alert('Copy failed. Please copy it manually.');
        });
    });
    
    //  Bootstrap toast (server-side)
    const serverToast = document.getElementById('serverToast');
    if (serverToast && window.bootstrap) {
        bootstrap.Toast.getOrCreateInstance(serverToast).show();
    }

    //  Keyboard shortcut: Ctrl/Cmd + K → search
    $(document).on('keydown', function (e) {
        if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
            e.preventDefault();
            $globalSearch.focus().select();
        }
    });

});