// A code listing's copy (F164.1, Louis: "this must be an actual code block you can copy paste"): the code written to
// the clipboard exactly as the listing shows it. True when the browser allowed it.
export async function copy(text) {
    try { await navigator.clipboard.writeText(text); return true; } catch { return false; }
}
