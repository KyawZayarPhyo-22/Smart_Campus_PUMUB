
// INIT AOS
AOS.init({ duration: 700, once: true, easing: 'ease-out-cubic' });

// LOADER
window.addEventListener('load', () => {
    const loader = document.getElementById('loader');
    if (loader) {
        setTimeout(() => { loader.classList.add('hidden'); }, 1800);
    }
});

// THEME
let dark = false;
function toggleTheme() {
    dark = !dark;
    document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light');
    document.getElementById('themeIcon').className = dark ? 'fas fa-sun' : 'fas fa-moon';
}

// NAVBAR SCROLL
window.addEventListener('scroll', () => {
    const nav = document.getElementById('navbar');
    const bt = document.getElementById('backTop');
    if (window.scrollY > 80) { nav.classList.add('scrolled'); } else { nav.classList.remove('scrolled'); }
    if (window.scrollY > 400) { bt.classList.add('visible'); } else { bt.classList.remove('visible'); }
});

// MOBILE MENU
function toggleMobile() {
    document.getElementById('mobileMenu').classList.toggle('open');
}

// PARTICLES
(function () {
    const container = document.getElementById('particles');
    if (!container) return;
    const colors = ['rgba(37,99,235,0.4)', 'rgba(6,182,212,0.4)', 'rgba(245,158,11,0.3)', 'rgba(255,255,255,0.2)'];
    for (let i = 0; i < 20; i++) {
        const p = document.createElement('div');
        const size = Math.random() * 30 + 10;
        p.className = 'particle';
        p.style.cssText = `width:${size}px;height:${size}px;left:${Math.random() * 100}%;background:${colors[Math.floor(Math.random() * colors.length)]};animation-duration:${Math.random() * 15 + 8}s;animation-delay:${Math.random() * 10}s;`;
        container.appendChild(p);
    }
})();

// TYPING ANIMATION
const phrases = ["Empowering Future Engineers", "Advancing Technology & Innovation", "Building Myanmar's Digital Future", "Excellence in Engineering Education"];
let pi = 0, ci = 0, deleting = false;
function typeText() {
    const el = document.getElementById('typingText');
    if (!el) return;
    const current = phrases[pi];
    if (!deleting) {
        el.innerHTML = current.substring(0, ci + 1) + '<span class="typing-cursor">|</span>';
        ci++;
        if (ci === current.length) { deleting = true; setTimeout(typeText, 2000); return; }
    } else {
        el.innerHTML = current.substring(0, ci - 1) + '<span class="typing-cursor">|</span>';
        ci--;
        if (ci === 0) { deleting = false; pi = (pi + 1) % phrases.length; }
    }
    setTimeout(typeText, deleting ? 60 : 90);
}
if (document.getElementById('typingText')) {
    typeText();
}

// COUNT UP ANIMATION
function animateCounters() {
    document.querySelectorAll('[data-count],.counter[data-count]').forEach(el => {
        const target = parseInt(el.getAttribute('data-count'));
        const suffix = el.getAttribute('data-count').includes('+') ? '+' : '';
        let current = 0;
        const duration = 2000;
        const step = target / (duration / 16);
        const timer = setInterval(() => {
            current += step;
            if (current >= target) { current = target; clearInterval(timer); }
            el.textContent = Math.floor(current).toLocaleString() + (target > 100 ? '+' : '');
        }, 16);
    });
}
const observer = new IntersectionObserver((entries) => {
    entries.forEach(e => { if (e.isIntersecting) { animateCounters(); observer.disconnect(); } });
}, { threshold: 0.3 });
const heroStats = document.querySelector('.hero-stats');
if (heroStats) observer.observe(heroStats);
const achiev = document.querySelector('#achievements');
if (achiev) {
    const achObserver = new IntersectionObserver((entries) => {
        entries.forEach(e => {
            if (e.isIntersecting) {
                e.target.querySelectorAll('.counter[data-count]').forEach(el => {
                    const target = parseInt(el.getAttribute('data-count'));
                    let current = 0;
                    const step = target / 80;
                    const timer = setInterval(() => {
                        current += step;
                        if (current >= target) { current = target; clearInterval(timer); }
                        el.textContent = Math.floor(current).toLocaleString() + '+';
                    }, 25);
                });
                achObserver.disconnect();
            }
        });
    }, { threshold: 0.3 });
    achObserver.observe(achiev);
}

// PROGRAMS FILTER
function filterPrograms(cat) {
    document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
    event.target.classList.add('active');
    document.querySelectorAll('.program-card').forEach(c => {
        if (cat === 'all' || c.getAttribute('data-category') === cat) {
            c.style.display = 'block';
            c.style.animation = 'fadeInUp 0.4s ease both';
        } else { c.style.display = 'none'; }
    });
}

// ADMISSION FORM
let curStep = 1;
function nextStep(step) {
    document.getElementById('formStep' + curStep).classList.remove('active');
    document.getElementById('formStep' + step).classList.add('active');
    for (let i = 1; i <= 4; i++) {
        const dot = document.getElementById('step' + i + 'dot');
        if (i < step) { dot.className = 'prog-dot done'; dot.innerHTML = '<i class="fas fa-check"></i>'; }
        else if (i === step) { dot.className = 'prog-dot active'; dot.textContent = i; }
        else { dot.className = 'prog-dot'; dot.textContent = i; }
    }
    document.getElementById('formProgressFill').style.width = ((step - 1) / 3 * 100) + '%';
    curStep = step;
}
function submitApp() {
    alert('🎉 Application submitted successfully!\n\nApplication ID: PUM-2025-' + Math.floor(Math.random() * 9000 + 1000) + '\n\nYou will receive a confirmation email within 24 hours. Good luck!');
    nextStep(1);
}

// AI CHAT RESPONSES
const aiResponses = {
    'apply': ['To apply to PU Maubin:\n1. Check eligibility (Matriculation with Distinction in Science)\n2. Fill the online application form above\n3. Prepare required documents (NRC, Matriculation certificate, photos)\n4. Take the entrance examination\n5. Await offer letter\n\nApplications for 2025-2026 are now open! 🎓', 'You can apply online using the application form in the Admissions section. The deadline is usually February of each year. Make sure you have passed Matriculation with Distinction in Physics, Chemistry, and Mathematics.'],
    'program': ['PU Maubin offers these B.E programs:\n• Civil Engineering (5 years)\n• Mechanical Engineering (5 years)\n• Electrical Engineering (5 years)\n• Electronic Engineering (5 years)\n• Information Technology (4 years)\n\nWe also offer M.E Postgraduate and Diploma programs! 📚', 'Our most popular program is Information Technology (B.E IT) with 350 seats. Civil and Electrical Engineering are also highly sought after.'],
    'scholarship': ['PU Maubin offers several scholarships:\n🏆 STEM Excellence Scholarship - Full tuition + accommodation\n📚 Merit Scholarship - 50% tuition reduction\n🌟 Regional Scholarship - For Ayeyarwady Region students\n\nApply through the Admissions office by March 31, 2025!', 'Scholarship applications are now open! Contact admissions@pu-maubin.edu.mm for details.'],
    'library': ['The PU Maubin Library is open:\n📅 Monday-Friday: 8:00 AM - 8:00 PM\n📅 Saturday: 8:00 AM - 5:00 PM\n📅 Sunday: Closed\n\nWe have 50,000+ books, 15,000+ e-books, and 8,000+ research papers! 📖', 'Our digital library is accessible 24/7 online through the student portal. Physical library hours are Mon-Fri 8AM-8PM, Sat 8AM-5PM.'],
    'default': ["I'm happy to help! I can answer questions about:\n• Admissions & Applications\n• Academic Programs\n• Scholarships\n• Library & Resources\n• Campus Life & Events\n• Research Programs\n\nWhat would you like to know? 😊", "Great question! Please ask me about our programs, admissions process, campus facilities, or anything else about PU Maubin. I'm here to help! 🎓"]
};
function getAiReply(msg) {
    msg = msg.toLowerCase();
    if (msg.includes('apply') || msg.includes('admission') || msg.includes('enroll')) return aiResponses.apply[Math.floor(Math.random() * aiResponses.apply.length)];
    if (msg.includes('program') || msg.includes('course') || msg.includes('study') || msg.includes('department')) return aiResponses.program[Math.floor(Math.random() * aiResponses.program.length)];
    if (msg.includes('scholarship') || msg.includes('fee') || msg.includes('financial')) return aiResponses.scholarship[Math.floor(Math.random() * aiResponses.scholarship.length)];
    if (msg.includes('library') || msg.includes('book') || msg.includes('hour')) return aiResponses.library[Math.floor(Math.random() * aiResponses.library.length)];
    return aiResponses.default[Math.floor(Math.random() * aiResponses.default.length)];
}
function sendAiMsg() {
    const input = document.getElementById('aiInput');
    const msg = input.value.trim();
    if (!msg) return;
    input.value = '';
    const msgs = document.getElementById('aiMessages');
    msgs.innerHTML += `<div class="ai-msg user"><div class="msg-bubble">${msg}</div><div class="msg-time">Just now</div></div>`;
    msgs.innerHTML += `<div class="ai-msg bot"><div class="typing-indicator"><div class="typing-dot"></div><div class="typing-dot"></div><div class="typing-dot"></div></div></div>`;
    msgs.scrollTop = msgs.scrollHeight;
    setTimeout(() => {
        const typing = msgs.querySelector('.typing-indicator');
        if (typing) typing.parentElement.remove();
        const reply = getAiReply(msg);
        msgs.innerHTML += `<div class="ai-msg bot" style="animation:slideIn 0.3s ease"><div class="msg-bubble">${reply.replace(/\n/g, '<br>')}</div><div class="msg-time">Just now</div></div>`;
        msgs.scrollTop = msgs.scrollHeight;
    }, 1200);
}
function askQuestion(q) { document.getElementById('aiInput').value = q; sendAiMsg(); }

// WIDGET AI
function toggleWidget() {
    document.getElementById('aiWidgetPopup').classList.toggle('open');
}
function sendWidgetMsg() {
    const input = document.getElementById('widgetInput');
    const msg = input.value.trim();
    if (!msg) return;
    input.value = '';
    const msgs = document.getElementById('widgetMsgs');
    msgs.innerHTML += `<div class="widget-msg user">${msg}</div>`;
    msgs.innerHTML += `<div class="widget-msg bot"><i class="fas fa-spinner fa-spin"></i></div>`;
    msgs.scrollTop = msgs.scrollHeight;
    setTimeout(() => {
        const dots = msgs.querySelector('.fa-spinner');
        if (dots) dots.parentElement.remove();
        msgs.innerHTML += `<div class="widget-msg bot">${getAiReply(msg).split('\n')[0]}</div>`;
        msgs.scrollTop = msgs.scrollHeight;
    }, 1000);
}

// COUNTDOWN TIMERS
function updateCountdowns() {
    const targets = [
        { date: '2025-04-20', d: 'cd1-d', h: 'cd1-h', m: 'cd1-m', s: 'cd1-s' },
        { date: '2025-03-15', d: 'cd2-d', h: 'cd2-h', m: 'cd2-m', s: 'cd2-s' },
        { date: '2025-03-05', d: 'cd3-d', h: 'cd3-h', m: 'cd3-m', s: 'cd3-s' }
    ];
    targets.forEach(t => {
        const diff = new Date(t.date) - new Date();
        if (diff > 0) {
            const d = Math.floor(diff / 86400000);
            const h = Math.floor((diff % 86400000) / 3600000);
            const m = Math.floor((diff % 3600000) / 60000);
            const s = Math.floor((diff % 60000) / 1000);
            ['d', 'h', 'm', 's'].forEach(k => {
                const el = document.getElementById(t[k]);
                if (el) el.textContent = String(eval(k)).padStart(2, '0');
            });
        }
    });
}
updateCountdowns();
setInterval(updateCountdowns, 1000);

// SMOOTH ACTIVE NAV LINKS
const sections = document.querySelectorAll('section[id]');
window.addEventListener('scroll', () => {
    let current = '';
    sections.forEach(s => { if (window.scrollY >= s.offsetTop - 120) current = s.id; });
    document.querySelectorAll('.nav-links a').forEach(a => {
        a.classList.remove('active');
        if (a.getAttribute('href') === '#' + current) a.classList.add('active');
    });
});

