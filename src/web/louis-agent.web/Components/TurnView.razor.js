// A turn's code blocks (CL31, beside TurnView), highlighted on the site's highlight.js, which stays a script the site
// lists (window.hljs). Blazor replaces a block's markup only when its markdown changes, so finished blocks keep their
// highlighting and a block that is still streaming is redone as it grows.
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
