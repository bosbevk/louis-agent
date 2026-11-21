// A button's keys (F164.2): Ctrl, or Cmd on a Mac, with a letter, from anywhere on the page, press the button, so a
// key and a press go the same way. Ctrl+I opens a site's agent and Ctrl+K its search; each button keeps its own.
const held = new Map();

// Holds a button's keys; holding them again replaces the first.
export function attach(button, key) {
    detach(button);
    const press = (e) => {
        if (!(e.ctrlKey || e.metaKey) || e.key.toLowerCase() !== key) return;
        e.preventDefault();
        button.click();
    };
    held.set(button, press);
    document.addEventListener("keydown", press);
}

// Lets a button's keys go with it.
export function detach(button) {
    const press = held.get(button);
    if (!press) return;
    document.removeEventListener("keydown", press);
    held.delete(button);
}
