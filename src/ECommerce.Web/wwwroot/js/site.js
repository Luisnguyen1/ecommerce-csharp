// Nimbus storefront interactions — zero dependencies.

document.addEventListener('click', function (e) {
    // Theme toggle
    var themeBtn = e.target.closest('[data-theme-toggle]');
    if (themeBtn) {
        var isDark = document.documentElement.classList.toggle('dark');
        localStorage.setItem('theme', isDark ? 'dark' : 'light');
        return;
    }

    // Dropdown menus: one open at a time, close on outside click.
    var toggle = e.target.closest('[data-menu-toggle]');
    var menus = document.querySelectorAll('.menu:not(.hidden)');
    if (toggle) {
        var menu = document.querySelector(toggle.getAttribute('data-menu-toggle'));
        var willOpen = menu && menu.classList.contains('hidden');
        menus.forEach(function (m) { if (m !== menu) m.classList.add('hidden'); });
        if (menu) menu.classList.toggle('hidden', !willOpen);
        e.stopPropagation();
        return;
    }
    if (menus.length && !e.target.closest('.menu')) {
        menus.forEach(function (m) { m.classList.add('hidden'); });
    }

    // Generic show/hide (mobile nav)
    var generic = e.target.closest('[data-toggle]');
    if (generic) {
        var el = document.querySelector(generic.getAttribute('data-toggle'));
        if (el) el.classList.toggle('hidden');
    }

    // Toast dismiss
    if (e.target.closest('[data-toast-close]')) {
        var toast = e.target.closest('[data-toast]');
        if (toast) toast.remove();
    }
});

// Confirm dangerous form submissions
document.querySelectorAll('form[data-confirm]').forEach(function (form) {
    form.addEventListener('submit', function (e) {
        if (!window.confirm(form.getAttribute('data-confirm'))) e.preventDefault();
    });
});

// Selects that submit their form on change (sort, filters)
document.querySelectorAll('[data-autosubmit]').forEach(function (select) {
    select.addEventListener('change', function () {
        var form = select.closest('form');
        if (form) form.submit();
    });
});

// Quantity steppers: <button data-stepper="#qty-input" data-step="1">
document.querySelectorAll('[data-stepper]').forEach(function (btn) {
    btn.addEventListener('click', function () {
        var input = document.querySelector(btn.getAttribute('data-stepper'));
        if (!input) return;
        var step = parseInt(btn.getAttribute('data-step') || '1', 10);
        var value = (parseInt(input.value || '0', 10) || 0) + step;
        var min = parseInt(input.getAttribute('min') || '1', 10);
        var max = parseInt(input.getAttribute('max') || '999', 10);
        input.value = Math.min(max, Math.max(min, value));
        input.dispatchEvent(new Event('change', { bubbles: true }));
    });
});

// Auto-dismiss toasts
document.querySelectorAll('[data-toast]').forEach(function (toast) {
    setTimeout(function () {
        toast.style.transition = 'opacity 0.3s';
        toast.style.opacity = '0';
        setTimeout(function () { toast.remove(); }, 300);
    }, 4000);
});

// Variant pickers on the product page: <button data-variant="id" ...>
document.querySelectorAll('[data-variant]').forEach(function (btn) {
    btn.addEventListener('click', function () {
        var group = btn.closest('[data-variant-group]');
        if (!group) return;
        group.querySelectorAll('[data-variant]').forEach(function (b) {
            b.classList.remove('border-primary', 'ring-2', 'ring-ring/30');
            b.classList.add('border-input');
        });
        btn.classList.remove('border-input');
        btn.classList.add('border-primary', 'ring-2', 'ring-ring/30');
        var hidden = group.closest('form')?.querySelector('input[name="variantId"]');
        if (hidden) hidden.value = btn.getAttribute('data-variant');
    });
});
