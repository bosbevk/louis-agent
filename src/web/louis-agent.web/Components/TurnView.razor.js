// A turn's code blocks (CL31, beside TurnView), highlighted on the site's highlight.js, which stays a script the site
// lists (window.hljs). Blazor replaces a block's markup only when its markdown changes, so finished blocks keep their
// highlighting and a block that is still streaming is redone as it grows. The files pack's viewer takes renderCode
// from here too: one whole file, the same highlighter.
export function highlightBlocks(container) {
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
}

// Fills an element Blazor leaves empty, so its re-renders never fight highlight.js over the markup. highlight.js
// escapes the text itself; very large files stay plain so the page doesn't stall.
export function renderCode(element, text, language) {
    if (!element) return;
    const hljs = window.hljs;
    if (hljs && language && text.length <= 300000 && hljs.getLanguage(language)) {
        element.innerHTML = hljs.highlight(text, { language, ignoreIllegals: true }).value;
    } else {
        element.textContent = text;
    }
}
