// The look switch's half in the browser (F151.3, a module beside its component, ADR 0093). A pick is written on the
// page's root element, where the theme's blocks read it, and remembered under the two words first-paint.js reads,
// so the next page and the next visit open the same way. Nothing is sent anywhere. A theme's id is checked before it
// touches the page, as SiteTheme checks it, and a style must be one of the two. A pick made in another tab of the
// same site reaches this one through the storage event, and the page and the switch follow it.
const themeKey = "site.theme";
const styleKey = "site.style";
const themeAttribute = "data-theme";
const styleAttribute = "data-style";
const styles = ["light", "dark"];

const root = document.documentElement;
const couldBeTheme = (id) => /^[a-z0-9-]+$/.test(id ?? "");
let follow = null;

// Kept in a try: a browser may refuse to keep anything, and the page still changes.
const remember = (key, word) => {
    try { localStorage.setItem(key, word); } catch { }
};

export function current() {
    return { theme: root.getAttribute(themeAttribute), style: root.getAttribute(styleAttribute) };
}

export function pickTheme(id) {
    if (!couldBeTheme(id)) return;
    root.setAttribute(themeAttribute, id);
    remember(themeKey, id);
}

export function pickStyle(word) {
    if (!styles.includes(word)) return;
    root.setAttribute(styleAttribute, word);
    remember(styleKey, word);
}

// Another tab changed the pick: put it on this page too, and tell the switch, so its pressed button follows.
export function listen(lookSwitch) {
    stop();
    follow = (event) => {
        const theme = event.key === themeKey && couldBeTheme(event.newValue);
        const style = event.key === styleKey && styles.includes(event.newValue);
        if (!theme && !style) return;
        root.setAttribute(theme ? themeAttribute : styleAttribute, event.newValue);
        const now = current();
        lookSwitch.invokeMethodAsync("Followed", now.theme, now.style);
    };
    window.addEventListener("storage", follow);
}

export function stop() {
    if (follow) window.removeEventListener("storage", follow);
    follow = null;
}
