// What this browser remembers about the page, applied before the first paint so nothing jumps: the sidebar's width,
// whether it or the work panel is hidden, and whether the page may move although the system asks for less. A site
// lists this in its head, before its stylesheet settles.
try {
    const width = parseInt(localStorage.getItem("chat.sidebarWidth"), 10);
    if (width >= 220 && width <= 480) document.documentElement.style.setProperty("--sidebar-w", width + "px");
    if (localStorage.getItem("chat.sidebarHidden") === "1") document.documentElement.classList.add("sidebar-hidden");
    if (localStorage.getItem("chat.workHidden") === "1") document.documentElement.classList.add("work-hidden");
    if (localStorage.getItem("chat.motion") === "on") document.documentElement.dataset.motion = "on";
} catch { }
