/* ==========================================================
   BTVH STORE PRO - MAIN JS
   ========================================================== */

// ===== REVEAL ON SCROLL =====
function setupReveal() {
    const els = document.querySelectorAll('.reveal:not(.show)');
    if (!('IntersectionObserver' in window)) { els.forEach(e => e.classList.add('show')); return; }
    const obs = new IntersectionObserver((entries) => {
        entries.forEach(e => {
            if (e.isIntersecting) { e.target.classList.add('show'); obs.unobserve(e.target); }
        });
    }, { threshold: 0.08, rootMargin: '0px 0px -24px 0px' });
    els.forEach(e => obs.observe(e));
}

// ===== MAGNETIC =====
function setupMagnetic() {
    document.querySelectorAll('.magnetic').forEach(el => {
        el.addEventListener('pointermove', e => {
            if (e.pointerType === 'touch') return;
            const r = el.getBoundingClientRect();
            el.style.transform = `translate(${(e.clientX - r.left - r.width/2) / 20}px, ${(e.clientY - r.top - r.height/2) / 20}px)`;
        });
        el.addEventListener('pointerleave', () => { el.style.transform = ''; });
    });
}

// ===== THEME TOGGLE =====
function setupTheme() {
    const btn = document.getElementById('theme-toggle');
    if (!btn) return;
    btn.addEventListener('click', () => {
        const cur = document.documentElement.getAttribute('data-theme') || 'light';
        const next = cur === 'light' ? 'dark' : 'light';
        document.documentElement.setAttribute('data-theme', next);
        localStorage.setItem('btvh-theme', next);
    });
}

// ===== BACK TO TOP =====
function setupBackTop() {
    const btn = document.getElementById('back-top');
    if (!btn) return;
    window.addEventListener('scroll', () => {
        btn.classList.toggle('show', window.scrollY > 400);
        const header = document.getElementById('site-header');
        if (header) header.classList.toggle('scrolled', window.scrollY > 10);
    }, { passive: true });
    btn.addEventListener('click', () => window.scrollTo({ top: 0, behavior: 'smooth' }));
}

// ===== COUNTERS =====
function setupCounters() {
    const counters = document.querySelectorAll('.counter');
    if (!counters.length || !('IntersectionObserver' in window)) return;
    const obs = new IntersectionObserver((entries) => {
        entries.forEach(e => {
            if (!e.isIntersecting) return;
            const el = e.target;
            const target = parseInt(el.dataset.target);
            const duration = 1500;
            const start = performance.now();
            const animate = (now) => {
                const progress = Math.min((now - start) / duration, 1);
                const eased = 1 - Math.pow(1 - progress, 3);
                el.textContent = Math.floor(target * eased).toLocaleString('vi-VN');
                if (progress < 1) requestAnimationFrame(animate);
                else el.textContent = target.toLocaleString('vi-VN');
            };
            requestAnimationFrame(animate);
            obs.unobserve(el);
        });
    }, { threshold: 0.3 });
    counters.forEach(c => obs.observe(c));
}

// ===== TYPEWRITER =====
function setupTypewriter() {
    const el = document.getElementById('typewriter');
    if (!el) return;
    const words = ['thông minh', 'tiện lợi', 'hiện đại', 'chất lượng'];
    let wi = 0, ci = 0, deleting = false;
    function tick() {
        const word = words[wi];
        if (!deleting) {
            el.textContent = word.substring(0, ci + 1);
            ci++;
            if (ci === word.length) { deleting = true; setTimeout(tick, 1800); return; }
        } else {
            el.textContent = word.substring(0, ci - 1);
            ci--;
            if (ci === 0) { deleting = false; wi = (wi + 1) % words.length; }
        }
        setTimeout(tick, deleting ? 50 : 100);
    }
    tick();
}

// ===== HERO PARALLAX =====
function setupParallax() {
    const bg = document.getElementById('hero-bg');
    if (!bg) return;
    window.addEventListener('scroll', () => {
        const y = window.scrollY;
        if (y < window.innerHeight) bg.style.transform = `translateY(${y * 0.3}px)`;
    }, { passive: true });
}

// ===== COUNTDOWN =====
function setupCountdown() {
    const el = document.querySelector('.countdown');
    if (!el) return;
    const seconds = parseInt(el.dataset.endsIn || 86400);
    let remaining = seconds;
    function tick() {
        const h = Math.floor(remaining / 3600);
        const m = Math.floor((remaining % 3600) / 60);
        const s = remaining % 60;
        document.getElementById('cd-h').textContent = String(h).padStart(2, '0');
        document.getElementById('cd-m').textContent = String(m).padStart(2, '0');
        document.getElementById('cd-s').textContent = String(s).padStart(2, '0');
        if (remaining > 0) { remaining--; setTimeout(tick, 1000); }
    }
    tick();
}

// ===== SEARCH SUGGESTIONS =====
function setupSearch() {
    const input = document.getElementById('search-input');
    const suggest = document.getElementById('search-suggest');
    if (!input || !suggest) return;
    let timer;
    input.addEventListener('input', () => {
        clearTimeout(timer);
        const q = input.value.trim();
        if (q.length < 2) { suggest.classList.remove('active'); return; }
        timer = setTimeout(async () => {
            try {
                const res = await fetch(`/san-pham?q=${encodeURIComponent(q)}&partial=1`);
                // Fallback: just show link
                suggest.innerHTML = `<a href="/san-pham?q=${encodeURIComponent(q)}" class="search-suggest-item">🔍 Tìm "${q}"</a>`;
                suggest.classList.add('active');
            } catch { suggest.classList.remove('active'); }
        }, 300);
    });
    document.addEventListener('click', e => {
        if (!suggest.contains(e.target) && e.target !== input) suggest.classList.remove('active');
    });
}

// ===== AJAX ADD TO CART =====
function setupAjaxCart() {
    document.querySelectorAll('form[action="/gio-hang/them"]').forEach(form => {
        form.addEventListener('submit', async (e) => {
            if (!window.fetch) return;
            e.preventDefault();
            const btn = form.querySelector('button[type=submit]');
            if (btn) { btn.disabled = true; btn.textContent = 'Đang thêm...'; }
            try {
                const res = await fetch(form.action, {
                    method: 'POST',
                    body: new FormData(form),
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    credentials: 'same-origin'
                });
                const data = await res.json();
                showToast(data.msg, data.ok ? 'success' : 'error');
                if (data.ok) {
                    updateCartBadge(data.count);
                    if (btn) btn.textContent = '✓ Đã thêm';
                    setTimeout(() => { if (btn) { btn.textContent = '🛒 Thêm giỏ'; btn.disabled = false; } }, 1500);
                    return;
                }
            } catch { form.submit(); }
            if (btn) { btn.disabled = false; btn.textContent = '🛒 Thêm giỏ'; }
        });
    });
}

function updateCartBadge(count) {
    const cartBtn = document.querySelector('.cart-btn');
    if (!cartBtn) return;
    let badge = cartBtn.querySelector('.icon-badge');
    if (!badge && count > 0) {
        badge = document.createElement('span');
        badge.className = 'icon-badge';
        cartBtn.appendChild(badge);
    }
    if (badge) {
        badge.textContent = count;
        if (count === 0) badge.remove();
    }
}

// ===== TOAST =====
function showToast(msg, type = 'success') {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container';
        document.body.appendChild(container);
    }
    const el = document.createElement('div');
    el.className = 'toast toast-' + type;
    el.innerHTML = `
        <span class="toast-icon">${type === 'success' ? '✓' : '!'}</span>
        <span class="toast-text">${escapeHtml(msg)}</span>
        <button class="toast-close" onclick="this.parentElement.remove()">×</button>
    `;
    container.appendChild(el);
    setTimeout(() => el.remove(), 4000);
}

function escapeHtml(s) {
    return String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

// ===== SIDEBAR TOGGLE (admin) =====
function setupSidebar() {
    const btn = document.getElementById('sidebar-toggle');
    const sidebar = document.getElementById('admin-sidebar');
    if (btn && sidebar) btn.addEventListener('click', () => sidebar.classList.toggle('open'));
}

// ===== SKELETON LOADING =====
function setupSkeleton() {
    document.querySelectorAll('[data-skeleton]').forEach(el => {
        el.classList.add('skeleton');
        setTimeout(() => el.classList.remove('skeleton'), 600);
    });
}

// ===== AUTO DISMISS ALERTS =====
function setupAlerts() {
    setTimeout(() => {
        document.querySelectorAll('.toast').forEach(t => {
            t.style.transition = 'all .3s';
            t.style.opacity = '0';
            t.style.transform = 'translateX(100%)';
            setTimeout(() => t.remove(), 300);
        });
    }, 4500);
}

// ===== INIT =====
document.addEventListener('DOMContentLoaded', () => {
    setupReveal();
    setupMagnetic();
    setupTheme();
    setupBackTop();
    setupCounters();
    setupTypewriter();
    setupParallax();
    setupCountdown();
    setupSearch();
    setupAjaxCart();
    setupSidebar();
    setupSkeleton();
    setupAlerts();
    document.body.classList.add('page-enter');
});

// Re-run reveal on dynamic content
window.refreshReveal = setupReveal;
