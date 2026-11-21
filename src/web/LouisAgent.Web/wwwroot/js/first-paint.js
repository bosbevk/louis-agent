// What this browser remembers about the page, applied before the first paint so nothing jumps: the theme and the
// style the visitor picked, the sidebar's width, whether it or the work panel is hidden, and whether the page may
// move although the system asks for less. A site lists this in its head, before its stylesheet settles.
try {
    // The look (F151.1): a style only when it is one of the two, a theme only when its id could be a theme's. With
    // nothing remembered the page keeps what its file wrote. The ?? "" is there because a regular expression
    // tests null as the word "null", which would pass.
    const style = localStorage.getItem("site.style");
    if (["light", "dark"].includes(style)) document.documentElement.setAttribute("data-style", style);
    const theme = localStorage.getItem("site.theme");
    if (/^[a-z0-9-]+$/.test(theme ?? "")) document.documentElement.setAttribute("data-theme", theme);
    const width = parseInt(localStorage.getItem("chat.sidebarWidth"), 10);
    if (width >= 220 && width <= 480) document.documentElement.style.setProperty("--sidebar-w", width + "px");
    if (localStorage.getItem("chat.sidebarHidden") === "1") document.documentElement.classList.add("sidebar-hidden");
    if (localStorage.getItem("chat.workHidden") === "1") document.documentElement.classList.add("work-hidden");
    if (localStorage.getItem("chat.motion") === "on") document.documentElement.dataset.motion = "on";
} catch { }
