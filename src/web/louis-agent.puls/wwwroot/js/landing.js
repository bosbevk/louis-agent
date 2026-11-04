// The landing page's motion (F24): the progress line, the compact nav, the section index, in-page links that land their
// section centred, reveals when blocks enter, counters, labels that decode from glyphs, the magnetic pill, the pinned
// tour and the demo that plays a turn. Started by LandingPage.razor after its first render and stopped when the page is
// left, since the page is a Blazor route: nothing here runs on DOMContentLoaded. Everything honours the system's
// "less motion" setting unless the viewer switched motion on (puls.motion in this browser, read by index.html too).
window.pulsLanding = (() => {
    const $ = (s, r = document) => r.querySelector(s);
    const $$ = (s, r = document) => [...r.querySelectorAll(s)];
    const clamp = v => Math.max(0, Math.min(1, v));
    const rand = s => s.replace(".", ",");          // Afrikaans decimals
    let cleanup = [];
    const on = (target, type, fn, opts) => { target.addEventListener(type, fn, opts); cleanup.push(() => target.removeEventListener(type, fn, opts)); };
    let still = false;

    // Every in-page link lands its section centred on the screen when it fits, else just under the nav; one smooth move.
    const navOffset = 96;
    function goTo(target) {
        const r = target.getBoundingClientRect(), fits = r.height <= innerHeight - navOffset;
        const top = scrollY + r.top - (fits ? (innerHeight - r.height) / 2 : navOffset);
        scrollTo({ top: Math.max(0, top), behavior: still ? "auto" : "smooth" });
    }
    function links() {
        $$(".landing a[href^='#'], .index a[href^='#']").forEach(a => on(a, "click", e => {
            const id = a.getAttribute("href");
            if (id === "#top") { e.preventDefault(); scrollTo({ top: 0, behavior: still ? "auto" : "smooth" }); return; }
            const target = $(id); if (!target) return;
            e.preventDefault(); goTo(target);
        }));
    }

    // Scroll: the progress line, the compact nav, the active section in the nav and in the index.
    function spy(anchors) {
        let active = -1;
        anchors.forEach((a, k) => { const s = $(a.getAttribute("href")); if (s && s.getBoundingClientRect().top < innerHeight * .45) active = k; });
        anchors.forEach((a, k) => a.classList.toggle("active", k === active));
    }
    function scrolling() {
        const progress = $("#landing-progress"), nav = $(".landing-nav"), navLinks = $$(".landing-links a"), indexLinks = $$(".index a");
        const onScroll = () => {
            const max = document.documentElement.scrollHeight - innerHeight;
            if (progress) progress.style.width = (max > 0 ? scrollY / max * 100 : 0) + "%";
            nav?.classList.toggle("compact", scrollY > 40);
            spy(navLinks); spy(indexLinks); pinScroll(); scrub();
        };
        on(window, "scroll", onScroll, { passive: true }); on(window, "resize", onScroll);
        return onScroll;
    }

    // Reveals, counters and the labels' decode: once, when a block comes into view.
    function reveals() {
        const seen = new IntersectionObserver(entries => entries.forEach(e => {
            if (!e.isIntersecting) return;
            e.target.classList.add("in"); seen.unobserve(e.target);
            if (e.target.classList.contains("eyebrow")) scramble(e.target);
            $$(".counter", e.target).forEach(count);
        }), { threshold: .25 });
        $$(".landing .reveal, .landing .reveal-group").forEach(el => seen.observe(el));
        cleanup.push(() => seen.disconnect());
        $$(".landing .bento .card, .landing .pricing .card, .landing .q").forEach(el => el.classList.add("focus"));
    }
    const glyphs = "▓▒░#%&@01<>/\\|=+*";
    function scramble(el) {
        if (still || el.dataset.done) return; el.dataset.done = "1";
        const text = el.textContent, t0 = performance.now(), d = 900;
        const tick = t => {
            const p = Math.min(1, (t - t0) / d), n = Math.floor(p * text.length);
            el.textContent = text.slice(0, n) + [...text.slice(n)].map(c => c === " " ? " " : glyphs[Math.random() * glyphs.length | 0]).join("");
            if (p < 1) requestAnimationFrame(tick); else el.textContent = text;
        };
        requestAnimationFrame(tick);
    }
    function count(el) {
        const to = parseFloat(el.dataset.to), fmt = el.dataset.fmt || "rand";
        const show = v => el.textContent = fmt === "rand" ? "R" + rand(v.toFixed(2)) : fmt === "pct" ? "−" + Math.round(v) + "%" : String(Math.round(v));
        if (still) { show(to); return; }
        const t0 = performance.now(), d = 1400;
        const tick = t => { const p = Math.min(1, (t - t0) / d), e = 1 - Math.pow(1 - p, 3); show(to * e); if (p < 1) requestAnimationFrame(tick); };
        requestAnimationFrame(tick);
    }

    // The headline, word by word; the gradient line comes in as one piece (a filter on text clipped to a background
    // makes the browser drop the text).
    function headline() {
        const h1 = $(".landing .hero h1"); if (!h1 || h1.dataset.split) return; h1.dataset.split = "1";
        let i = 0;
        const split = node => {
            if (node.nodeType === 1 && node.classList.contains("gradient-text")) { node.classList.add("w"); node.style.setProperty("--i", i++); return; }
            if (node.nodeType === 3) {
                const frag = document.createDocumentFragment();
                node.textContent.split(/(\s+)/).forEach(part => {
                    if (!part) return;
                    if (/^\s+$/.test(part)) { frag.appendChild(document.createTextNode(" ")); return; }
                    const s = document.createElement("span"); s.className = "w"; s.style.setProperty("--i", i++); s.textContent = part; frag.appendChild(s);
                });
                node.replaceWith(frag);
            } else [...node.childNodes].forEach(split);
        };
        split(h1);
    }

    // Vrae: one question open at a time; the first starts open.
    function faq() {
        const qs = $$(".landing .q"); if (qs.length === 0) return;
        const set = (q, open) => { q.classList.toggle("open", open); $("button", q)?.setAttribute("aria-expanded", String(open)); const body = $(".body", q); if (body) body.inert = !open; };
        qs.forEach(q => on($("button", q), "click", () => { const open = !q.classList.contains("open"); qs.forEach(o => set(o, o === q && open)); }));
        qs.forEach(q => set(q, false)); set(qs[0], true);
    }

    // The primary pills follow the pointer a few pixels and spring back.
    function magnetic() {
        if (still || !matchMedia("(pointer: fine)").matches) return;
        $$(".landing .magnet").forEach(b => {
            on(b, "pointermove", e => { const r = b.getBoundingClientRect(); b.style.transform = `translate(${(e.clientX - r.left - r.width / 2) * .18}px, ${(e.clientY - r.top - r.height / 2) * .28}px)`; });
            on(b, "pointerleave", () => b.style.transform = "");
        });
    }

    // The app's frame lies tilted back and flattens as it scrolls into view, where the browser has no scroll-driven CSS.
    let showcase = null;
    const scrubs = !CSS.supports("animation-timeline: view()");
    function scrub() {
        if (still || !scrubs || !showcase) return;
        const r = showcase.getBoundingClientRect(); showcase.style.setProperty("--p", clamp((innerHeight - r.top) / (innerHeight * .85)));
    }

    // In the app, pinned: the scroll through the tall block picks the chip; a click scrolls to that chip's part of it.
    let pin = null, chips = [], panels = [], tab = -1, tabInfo = [];
    function showTab(i) {
        if (i === tab || !tabInfo[i]) return; tab = i;
        chips.forEach((c, k) => { c.classList.toggle("active", k === i); c.setAttribute("aria-pressed", String(k === i)); });
        panels.forEach((p, k) => p.classList.toggle("active", k === i));
        const title = $("#pin-title"), text = $("#pin-text");
        if (title) title.textContent = tabInfo[i][0]; if (text) text.textContent = tabInfo[i][1];
    }
    function pinScroll() {
        if (!pin || innerWidth <= 900) return;
        const total = pin.offsetHeight - innerHeight, p = clamp(-pin.getBoundingClientRect().top / Math.max(1, total));
        showTab(Math.min(chips.length - 1, Math.floor(p * chips.length)));
    }
    function pinned() {
        pin = $("#pin"); if (!pin) return;
        chips = $$("#chips button"); panels = $$("#pin-frame .panel");
        tabInfo = chips.map(c => [c.dataset.title || c.textContent.trim(), c.dataset.text || ""]);
        chips.forEach((c, k) => on(c, "click", () => {
            if (innerWidth <= 900) showTab(k);
            else scrollTo({ top: pin.getBoundingClientRect().top + scrollY + (pin.offsetHeight - innerHeight) * (k + .5) / chips.length, behavior: still ? "auto" : "smooth" });
        }));
        tab = -1; showTab(0);
    }

    // The demo: a turn, step by step, from the turns the page gives it (data-turns on the frame). It starts once the
    // frame is well into view (Die app), at a reading pace. Reduced motion keeps the pace, since text arriving in order
    // is content and not movement; it only plays the one turn instead of looping.
    let playing = false, n = 0, demoTimers = [];
    const sleep = ms => new Promise(r => { const t = setTimeout(r, ms); demoTimers.push(t); });
    async function play(frame, t) {
        const messages = $(".demo-messages", frame), composer = $(".demo-composer", frame), typed = $(".text", composer), send = $(".send", composer), work = $(".work-body", frame), wcount = $(".work-count", frame);
        if (!messages) return;
        playing = true; $(".replay:not(.demo-stop)")?.setAttribute("hidden", ""); $(".demo-stop")?.removeAttribute("hidden");
        messages.innerHTML = ""; work.innerHTML = ""; wcount.textContent = "0"; typed.textContent = "";
        $$(".demo-chat-item", frame).forEach(c => c.classList.toggle("active", c.dataset.chat == t.chat));
        composer.classList.add("focus");
        for (const ch of t.q) { typed.textContent += ch; await sleep(45 + Math.random() * 40); }
        send.classList.add("go"); await sleep(700);
        send.classList.remove("go"); composer.classList.remove("focus"); typed.textContent = "";
        messages.insertAdjacentHTML("beforeend", `<div class="user-message"><div class="user-text">${t.q}</div></div>`);
        await sleep(900);
        // The app's own pieces, so the demo looks exactly like a real turn: a delegation card and a Werk item.
        const chips = t.sets.map(s => `<span class="toolset-chip">${s}</span>`).join("");
        const tile = frame.dataset.tile || "brand/puls-icon-tile.svg";
        messages.insertAdjacentHTML("beforeend", `<div class="delegation running demo-card"><div class="delegation-head"><span class="delegation-avatar"></span><div class="delegation-title"><span class="delegation-agent">Oorgedra aan <strong>puls</strong></span><div class="delegation-toolsets">${chips}</div></div><span class="delegation-status">Besig</span></div></div>`);
        work.insertAdjacentHTML("beforeend", `<div class="work-item running demo-item"><div class="work-item-head"><img class="work-avatar" src="${tile}" alt=""><div class="work-who"><small>Spesialis</small><strong>puls</strong></div><span class="work-status">Besig</span></div><div class="work-chips">${chips}</div><ul class="work-steps demo-steps"></ul></div>`);
        wcount.textContent = "1";
        const steps = $(".demo-steps", frame);
        for (const [name, args] of t.steps) {
            steps.insertAdjacentHTML("beforeend", `<li class="work-step waiting"><span class="work-step-mark">…</span><span class="work-step-name">${name}</span><span class="work-step-args">${args}</span></li>`);
            await sleep(1100 + Math.random() * 700);
            const li = steps.lastElementChild; li.classList.remove("waiting"); $(".work-step-mark", li).textContent = "✓";
        }
        await sleep(700);
        messages.insertAdjacentHTML("beforeend", `<div class="demo-answer markdown"><span class="demo-caret"></span></div>`);
        const answer = $(".demo-answer", frame), words = t.a.split(" ");
        let out = "";
        for (const w of words) { out += (out ? " " : "") + w; answer.innerHTML = out + '<span class="demo-caret"></span>'; await sleep(70 + Math.random() * 80); }
        answer.innerHTML = out;
        const card = $(".demo-card", frame); card.classList.remove("running"); $(".delegation-status", card).textContent = "Klaar";
        const wi = $(".demo-item", frame); wi.classList.remove("running"); $(".work-status", wi).textContent = "Klaar";
        wi.insertAdjacentHTML("beforeend", `<span class="work-label">Bronne</span>${t.src.map(s => `<span class="demo-source">${s}</span>`).join("")}<div class="work-cost"><span>${t.req} versoeke</span><strong class="demo-cost">R0,00</strong></div>`);
        const costn = $(".demo-cost", frame), t0 = performance.now();
        await new Promise(res => { const tick = tm => { const p = Math.min(1, (tm - t0) / 1200), e = 1 - Math.pow(1 - p, 3); costn.textContent = "R" + rand((t.cost * e).toFixed(2)); p < 1 ? requestAnimationFrame(tick) : res(); }; requestAnimationFrame(tick); });
        messages.insertAdjacentHTML("beforeend", `<div class="usage">beurt: ${t.req + 2} versoeke · R${rand((t.cost + 0.01).toFixed(2))} · gesprek R${rand((t.cost + 0.01).toFixed(2))}</div>`);
        playing = false;
    }
    async function loop(frame, turns) {
        for (; n < turns.length; n++) { await play(frame, turns[n]); if (!frame.isConnected) return; await sleep(5000); if (still) break; }
        n = 0; $(".demo-stop")?.setAttribute("hidden", ""); $(".replay:not(.demo-stop)")?.removeAttribute("hidden");
    }
    // Stop: the pending beat's timer is cleared, so the turn's chain never continues; what is on screen stays.
    function stopDemo() {
        demoTimers.forEach(clearTimeout); demoTimers = []; playing = false; n = 0;
        $(".demo-stop")?.setAttribute("hidden", ""); $(".replay:not(.demo-stop)")?.removeAttribute("hidden");
    }
    function demo() {
        const frame = $("#landing-demo"); if (!frame) return;
        showcase = frame.closest(".showcase");
        let turns = []; try { turns = JSON.parse(frame.dataset.turns || "[]"); } catch { }
        if (turns.length === 0) return;
        const replay = $(".replay:not(.demo-stop)"), stop = $(".demo-stop");
        if (replay) on(replay, "click", () => { if (!playing) loop(frame, turns); });
        if (stop) on(stop, "click", stopDemo);
        const once = () => play(frame, turns[0]).then(() => { stop?.setAttribute("hidden", ""); replay?.removeAttribute("hidden"); });
        const start = new IntersectionObserver(e => { if (e[0].isIntersecting) { start.disconnect(); still ? once() : loop(frame, turns); } }, { threshold: .6 });
        // The marquees under Kenmerke: a button pauses them (they also pause under the pointer); reduced motion stills them anyway.
        const pause = $(".marquee-pause"), marquee = $(".landing .marquee");
        if (pause && marquee) on(pause, "click", () => { const paused = marquee.classList.toggle("paused"); pause.setAttribute("aria-pressed", String(paused)); pause.textContent = paused ? "Speel die lys" : "Pouse die lys"; });
        start.observe(frame); cleanup.push(() => start.disconnect());
    }

    return {
        start() {
            this.stop();
            still = matchMedia("(prefers-reduced-motion: reduce)").matches && document.documentElement.dataset.motion !== "on";
            headline(); links(); reveals(); magnetic(); pinned(); demo(); faq();
            const onScroll = scrolling(); onScroll();
            // The system asks for less motion and the viewer hasn't chosen: offer the animations; a yes applies at once
            // (pulsUi.setMotion in chat.js remembers it) and this page starts over with them.
            const pill = $("#motion-pill");
            if (pill) { pill.hidden = !still; if (still) on(pill, "click", () => { window.pulsUi?.setMotion(true); pill.hidden = true; this.start(); }); }
        },
        stop() {
            cleanup.forEach(f => f()); cleanup = [];
            demoTimers.forEach(clearTimeout); demoTimers = []; playing = false; n = 0; pin = null; showcase = null; tab = -1;
        },
    };
})();
