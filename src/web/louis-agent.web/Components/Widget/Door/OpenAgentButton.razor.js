// The keys that open a site's agent (F164.2): Ctrl+I, Cmd+I on a Mac, from anywhere on the page, press the header's
// button, so a key and a press go the same way through the door.
let held = null;

const openOnKeys = (e) => {
    if (!(e.ctrlKey || e.metaKey) || e.key.toLowerCase() !== "i") return;
    e.preventDefault();
    held?.click();
};

// Holds the button the keys press; a second attach replaces the first.
export function attach(button) {
    detach();
    held = button;
    document.addEventListener("keydown", openOnKeys);
}

// Lets the keys go with the button.
export function detach() {
    document.removeEventListener("keydown", openOnKeys);
    held = null;
}
