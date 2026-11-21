// Reset my session (F153): what this browser keeps, for the reset to read and then forget.

/** The keys of every list of chats this browser saved: the chat page's, the guide's, the showroom's, and any later. */
export function chatKeys() {
    const keys = [];
    for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key?.endsWith("chats")) keys.push(key);
    }
    return keys;
}

/** Forgets everything this browser keeps for the site, in both storages, but the one key it is told to keep. */
export function forget(keep) {
    const kept = localStorage.getItem(keep);
    localStorage.clear();
    sessionStorage.clear();
    if (kept !== null) localStorage.setItem(keep, kept);
}
