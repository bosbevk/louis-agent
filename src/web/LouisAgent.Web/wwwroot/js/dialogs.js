// Focus for what Blazor has just drawn (the framework's, CL31): a dialog, so Esc reaches it and a screen reader starts
// there, or a field such as the rename box, with its text selected when asked.
export function focus(selector, select) {
    const field = document.querySelector(selector);
    if (!field) return;
    field.focus();
    if (select) field.select();
}
