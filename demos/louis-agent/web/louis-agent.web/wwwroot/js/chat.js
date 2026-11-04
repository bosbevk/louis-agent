window.louisChat = {
    // Follows the stream only while the reader is already at the bottom, so scrolling up to read isn't interrupted.
    scrollToBottom(element, force) {
        if (!element) return;
        const nearBottom = element.scrollHeight - element.scrollTop - element.clientHeight < 120;
        if (force || nearBottom) element.scrollTop = element.scrollHeight;
    },

    // Fills an element Blazor leaves empty, so its re-renders never fight highlight.js over the markup. highlight.js
    // escapes the text itself; very large files stay plain so the page doesn't stall.
    renderCode(element, text, language) {
        if (!element) return;
        const hljs = window.hljs;
        if (hljs && language && text.length <= 300000 && hljs.getLanguage(language)) {
            element.innerHTML = hljs.highlight(text, { language, ignoreIllegals: true }).value;
        } else {
            element.textContent = text;
        }
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
                // No language given: let highlight.js guess, but only for blocks small enough to guess quickly.
                code.innerHTML = hljs.highlightAuto(code.textContent).value;
                code.dataset.highlighted = "auto";
            }
        }
    },

    // Themes are css/themes/<id>.css; ids are checked so only a file from that folder can ever be loaded.
    getTheme() {
        return document.documentElement.dataset.theme || "retro";
    },

    setTheme(id) {
        if (!/^[a-z0-9-]+$/.test(id)) return;
        document.getElementById("theme-css").href = "css/themes/" + id + ".css";
        document.documentElement.dataset.theme = id;
        try { localStorage.setItem("louis-agent.theme", id); } catch { }
    },

    focusComposer() {
        const input = document.getElementById("composer-input");
        if (!input) return;
        input.focus();
        input.setSelectionRange(input.value.length, input.value.length);
    },
};

// A theme picked in another tab applies here too, so open tabs never disagree.
window.addEventListener("storage", (e) => {
    if (e.key !== "louis-agent.theme" || !e.newValue || !/^[a-z0-9-]+$/.test(e.newValue)) return;
    document.getElementById("theme-css").href = "css/themes/" + e.newValue + ".css";
    document.documentElement.dataset.theme = e.newValue;
});

// Enter sends, Shift+Enter adds a new line (IME composition is left alone).
document.addEventListener("keydown", (e) => {
    if (e.target.id !== "composer-input" || e.key !== "Enter" || e.shiftKey || e.isComposing) return;
    e.preventDefault();
    document.getElementById("send-button")?.click();
});
