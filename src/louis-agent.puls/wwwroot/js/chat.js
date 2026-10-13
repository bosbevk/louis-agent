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

