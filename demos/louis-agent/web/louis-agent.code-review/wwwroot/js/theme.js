// The theme picker's half in the browser. Themes are css/themes/<id>.css; ids are checked so only a file from that folder
// can ever be loaded, and a theme picked in another tab applies here too, so open tabs never disagree.
(() => {
    const key = "louis-agent.theme";
    const valid = id => /^[a-z0-9-]+$/.test(id);

    function apply(id) {
        document.getElementById("theme-css").href = "css/themes/" + id + ".css";
        document.documentElement.dataset.theme = id;
    }

    window.siteTheme = {
        get() {
            return document.documentElement.dataset.theme || "retro";
        },

        set(id) {
            if (!valid(id)) return;
            apply(id);
            try { localStorage.setItem(key, id); } catch { }
        },
    };

    window.addEventListener("storage", e => {
        if (e.key === key && e.newValue && valid(e.newValue)) apply(e.newValue);
    });
})();
