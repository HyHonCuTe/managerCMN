/**
 * Unified notification renderer — all app notifications go through here.
 * API: window.showNotification(message, type)
 * Types: 'success' | 'info' | 'warning' | 'danger' | 'error' | 'forbidden' | 'blocked'
 */
(function () {
    'use strict';

    var CONTAINER_ID = 'app-notification-container';
    var DISMISS_MS   = 3000;
    var DEDUP_MS     = 400;

    var TYPE_CONFIG = {
        success:   { bg: '#198754', icon: 'bi-check-circle-fill' },
        info:      { bg: '#0d6efd', icon: 'bi-info-circle-fill' },
        warning:   { bg: '#e6a817', icon: 'bi-exclamation-triangle-fill' },
        danger:    { bg: '#dc3545', icon: 'bi-x-circle-fill' },
        error:     { bg: '#dc3545', icon: 'bi-x-circle-fill' },
        forbidden: { bg: '#dc3545', icon: 'bi-slash-circle-fill' },
    };

    // Recent message dedup map  {key -> true}
    var _recent = {};

    function getContainer() {
        var el = document.getElementById(CONTAINER_ID);
        if (!el) {
            el = document.createElement('div');
            el.id = CONTAINER_ID;
            el.setAttribute('aria-live', 'polite');
            el.setAttribute('aria-atomic', 'false');
            document.body.appendChild(el);
        }
        return el;
    }

    function isDuplicate(key) {
        if (_recent[key]) return true;
        _recent[key] = true;
        setTimeout(function () { delete _recent[key]; }, DEDUP_MS);
        return false;
    }

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function dismissEl(el) {
        if (!el || !el.parentElement) return;
        el.classList.remove('app-notif-visible');
        el.classList.add('app-notif-hiding');
        setTimeout(function () { if (el.parentElement) el.parentElement.removeChild(el); }, 280);
    }

    function showNotification(message, type) {
        if (!message || !String(message).trim()) return;

        type = type || 'info';
        var normalized = TYPE_CONFIG.hasOwnProperty(type) ? type : 'info';
        var key = normalized + ':' + message;
        if (isDuplicate(key)) return;

        var cfg = TYPE_CONFIG[normalized];
        var container = getContainer();
        var id = 'notif_' + Date.now() + '_' + Math.floor(Math.random() * 9999);

        var el = document.createElement('div');
        el.id = id;
        el.className = 'app-notif';
        el.setAttribute('role', 'alert');
        el.style.background = cfg.bg;

        el.innerHTML =
            '<i class="bi ' + cfg.icon + ' app-notif-icon"></i>' +
            '<span class="app-notif-text">' + escapeHtml(String(message)) + '</span>' +
            '<button class="app-notif-close" aria-label="Đóng"><i class="bi bi-x-lg"></i></button>';

        container.appendChild(el);

        // Animate in (next frame so transition fires)
        requestAnimationFrame(function () {
            requestAnimationFrame(function () { el.classList.add('app-notif-visible'); });
        });

        var timer = setTimeout(function () { dismissEl(el); }, DISMISS_MS);

        el.querySelector('.app-notif-close').addEventListener('click', function () {
            clearTimeout(timer);
            dismissEl(el);
        });
    }

    // On DOM ready: scan asp-validation-summary blocks and show errors as notifications
    function scanValidationSummary() {
        var summaries = document.querySelectorAll('[data-valmsg-summary="true"]');
        summaries.forEach(function (summary) {
            var items = summary.querySelectorAll('li');
            items.forEach(function (li) {
                var txt = (li.textContent || '').trim();
                if (txt) showNotification(txt, 'danger');
            });
            // Hide the inline block after extracting (notifications show top-right)
            if (items.length > 0) {
                summary.style.display = 'none';
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', scanValidationSummary);
    } else {
        scanValidationSummary();
    }

    window.showNotification = showNotification;
})();
