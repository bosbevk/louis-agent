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
