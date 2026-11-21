// The message box's own keys (FX70, a module beside its component, ADR 0093): Enter sends, Shift+Enter adds a new
// line, and a key pressed while an input method composes is left alone. The rule sits on the box itself, so it
// reaches a box outside the chat shell (the widget in a site's corner) and never fires twice for one box.
const enterSends = (e) => {
    if (e.key !== "Enter" || e.shiftKey || e.isComposing) return;
    e.preventDefault();
    e.target.closest("div")?.querySelector(".send")?.click();
};

// Hangs the key rule on one box; attaching twice to the same box is one listener, since the function is the same.
export function attach(textarea) {
    textarea?.addEventListener("keydown", enterSends);
}
