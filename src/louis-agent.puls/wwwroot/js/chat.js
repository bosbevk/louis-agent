window.louisChat = {
    // Follows the stream only while the reader is already at the bottom, so scrolling up to read isn't interrupted.
    scrollToBottom(element, force) {
        if (!element) return;
        const nearBottom = element.scrollHeight - element.scrollTop - element.clientHeight < 120;
        if (force || nearBottom) element.scrollTop = element.scrollHeight;
    },

    // Highlights fenced code blocks in a rendered answer. Blazor replaces a block's markup only when its markdown changes,
    // so finished blocks keep their highlighting and a block that is still streaming is redone as it grows.
    highlightBlocks(container) {
        const hljs = window.hljs;
        if (!hljs || !container) return;
        for (const code of container.querySelectorAll("pre code:not([data-highlighted])")) {
            const language = [...code.classList].find(c => c.startsWith("language-"))?.slice("language-".length);
            if (code.textContent.length > 100000 || (language && !hljs.getLanguage(language))) {
                code.dataset.highlighted = "skipped";
            } else if (language) {
                hljs.highlightElement(code);
            } else if (code.textContent.length <= 20000) {
                code.innerHTML = hljs.highlightAuto(code.textContent).value;
                code.dataset.highlighted = "auto";
            }
        }
    },
};

// Enter sends, Shift+Enter adds a new line (IME composition is left alone); the box grows with what's typed.
document.addEventListener("keydown", (e) => {
    if (e.target.id !== "composer-input" || e.key !== "Enter" || e.shiftKey || e.isComposing) return;
    e.preventDefault();
    document.getElementById("send-button")?.click();
});

document.addEventListener("input", (e) => {
    if (e.target.id !== "composer-input") return;
    e.target.style.height = "auto";
    e.target.style.height = Math.min(e.target.scrollHeight, 240) + "px";
});

// The sidebar: drag its edge to resize (220-480px, remembered), double-click the edge to reset, hide and show it.
window.pulsUi = {
    minWidth: 220,
    maxWidth: 480,
    defaultWidth: 280,

    setWidth(width) {
        const clamped = Math.round(Math.min(this.maxWidth, Math.max(this.minWidth, width)));
        document.documentElement.style.setProperty("--sidebar-w", clamped + "px");
        try { localStorage.setItem("puls.sidebarWidth", clamped); } catch { }
    },

    toggleSidebar() {
        const hidden = document.documentElement.classList.toggle("sidebar-hidden");
        try { localStorage.setItem("puls.sidebarHidden", hidden ? "1" : "0"); } catch { }
    },

    // The Werk panel on a wide screen (F20): hidden or shown, remembered.
    toggleWork() {
        const hidden = document.documentElement.classList.toggle("work-hidden");
        try { localStorage.setItem("puls.workHidden", hidden ? "1" : "0"); } catch { }
    },
};

document.addEventListener("pointerdown", (e) => {
    const handle = e.target.closest?.(".resizer");
    if (!handle) return;
    e.preventDefault();
    handle.setPointerCapture(e.pointerId);
    document.documentElement.classList.add("resizing");
    const move = (ev) => pulsUi.setWidth(ev.clientX);
    const up = () => {
        document.documentElement.classList.remove("resizing");
        handle.removeEventListener("pointermove", move);
        handle.removeEventListener("pointerup", up);
    };
    handle.addEventListener("pointermove", move);
    handle.addEventListener("pointerup", up);
});

document.addEventListener("dblclick", (e) => {
    if (e.target.closest?.(".resizer")) pulsUi.setWidth(pulsUi.defaultWidth);
});

// On a phone the sidebar is an offcanvas: close it once a chat is picked or a new one started.
document.addEventListener("click", (e) => {
    if (!e.target.closest?.(".chat-item a, .new-chat")) return;
    const sidebar = document.getElementById("sidebar");
    if (sidebar && window.bootstrap) window.bootstrap.Offcanvas.getInstance(sidebar)?.hide();
});

// The spotlight on cards follows the mouse (app.css reads --mx/--my).
document.addEventListener("pointermove", (e) => {
    const card = e.target.closest?.(".suggestion, .delegation, .tool");
    if (!card) return;
    const box = card.getBoundingClientRect();
    card.style.setProperty("--mx", (e.clientX - box.left) + "px");
    card.style.setProperty("--my", (e.clientY - box.top) + "px");
}, { passive: true });

// Focus a field once Blazor has drawn it (the chat search, the rename field); select its text when asked.
pulsUi.focus = (selector, select) => {
    const field = document.querySelector(selector);
    if (!field) return;
    field.focus();
    if (select) field.select();
};

// Ctrl K (Cmd K on a Mac) opens the chat search, wherever the focus is.
document.addEventListener("keydown", (e) => {
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
        e.preventDefault();
        document.getElementById("search-button")?.click();
    }
});

// The profile menu's Instellings: show the sidebar if it's hidden, then open its settings.
pulsUi.openSettings = () => {
    if (document.documentElement.classList.contains("sidebar-hidden")) pulsUi.toggleSidebar();
    const settings = document.querySelector(".sidebar-settings");
    if (!settings) return;
    settings.open = true;
    settings.scrollIntoView({ block: "end", behavior: "smooth" });
};

// Where the user is (F22): kept in this browser only.
pulsUi.loadLocation = () => { try { return localStorage.getItem("puls.location"); } catch { return null; } };
pulsUi.saveLocation = (place) => {
    try { if (place) localStorage.setItem("puls.location", place); else localStorage.removeItem("puls.location"); } catch { }
};
pulsUi.timeZone = () => { try { return Intl.DateTimeFormat().resolvedOptions().timeZone || null; } catch { return null; } };

// "Gebruik my ligging": the browser's position (it asks the user once), named by OpenStreetMap's Nominatim at town level.
// One request per press, as Nominatim's usage policy asks; the browser sends its own Referer and User-Agent.
pulsUi.locate = () => new Promise((resolve) => {
    if (!navigator.geolocation) return resolve({ place: null, error: "Hierdie blaaier kan nie jou ligging gee nie." });
    navigator.geolocation.getCurrentPosition(async (position) => {
        try {
            const { latitude, longitude } = position.coords;
            const url = `https://nominatim.openstreetmap.org/reverse?format=jsonv2&zoom=10&accept-language=af,en`
                + `&lat=${latitude.toFixed(4)}&lon=${longitude.toFixed(4)}`;
            const response = await fetch(url, { headers: { "Accept": "application/json" } });
            if (!response.ok) return resolve({ place: null, error: "OpenStreetMap kon nie die plek benoem nie." });
            const address = (await response.json()).address || {};
            const town = address.city || address.town || address.village || address.municipality || address.suburb || address.county;
            const place = [town, address.state || address.province, address.country].filter(Boolean).join(", ");
            resolve(place ? { place, error: null } : { place: null, error: "OpenStreetMap kon nie die plek benoem nie." });
        } catch {
            resolve({ place: null, error: "Kon nie OpenStreetMap bereik nie." });
        }
    }, (error) => resolve({
        place: null,
        error: error.code === 1 ? "Jy het nie toestemming gegee vir jou ligging nie." : "Kon nie jou ligging kry nie.",
    }), { timeout: 10000, maximumAge: 600000 });
});

// A starter in the message box (F20): focus it with the cursor at the end, and grow the box to fit.
pulsUi.focusEnd = (selector) => {
    const box = document.querySelector(selector);
    if (!box) return;
    box.focus();
    box.setSelectionRange(box.value.length, box.value.length);
    box.style.height = "auto";
    box.style.height = Math.min(box.scrollHeight, 240) + "px";
};

