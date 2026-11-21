// What this browser remembers for the person (the framework's, CL31; moved from chat.js): animations although the
// system asks for less, where they are, the language a new chat opens with, and the browser's own position named as
// a town. Nothing here leaves the browser until a new chat sends the place.

// Animations although the system asks for less (F24): data-motion="on" on <html> (app.css lifts its reduced-motion
// rules under it; first-paint.js applies it before the first paint), remembered in this browser.
export function motion() { return document.documentElement.dataset.motion === "on"; }
export function reducedMotion() { return matchMedia("(prefers-reduced-motion: reduce)").matches; }
export function setMotion(on) {
    if (on) document.documentElement.dataset.motion = "on"; else delete document.documentElement.dataset.motion;
    try { if (on) localStorage.setItem("chat.motion", "on"); else localStorage.removeItem("chat.motion"); } catch { }
}

// Where the user is (F22): kept in this browser only.
export function loadLocation() { try { return localStorage.getItem("chat.location"); } catch { return null; } }
export function saveLocation(place) {
    try { if (place) localStorage.setItem("chat.location", place); else localStorage.removeItem("chat.location"); } catch { }
}

// The language: the voice a new chat opens with, remembered in this browser.
export function loadLanguage() { try { return localStorage.getItem("chat.language"); } catch { return null; } }
export function saveLanguage(code) { try { localStorage.setItem("chat.language", code); } catch { } }

// The location button: the browser's position (it asks the user once), named by OpenStreetMap's Nominatim at town level.
// One request per press, as Nominatim's usage policy asks; the browser sends its own Referer and User-Agent. The position
// goes to two decimals (about 1 km), all a town needs.
export function locate(languages) {
    return new Promise((resolve) => {
        if (!navigator.geolocation) return resolve({ place: null, error: "no-browser" });
        navigator.geolocation.getCurrentPosition(async (position) => {
            try {
                const { latitude, longitude } = position.coords;
                const url = `https://nominatim.openstreetmap.org/reverse?format=jsonv2&zoom=10&accept-language=${encodeURIComponent(languages)}`
                    + `&lat=${latitude.toFixed(2)}&lon=${longitude.toFixed(2)}`;   // ~1 km: a town, not a street
                const response = await fetch(url, { headers: { "Accept": "application/json" } });
                if (!response.ok) return resolve({ place: null, error: "not-named" });
                const address = (await response.json()).address || {};
                const town = address.city || address.town || address.village || address.municipality || address.suburb || address.county;
                const place = [town, address.state || address.province, address.country].filter(Boolean).join(", ");
                resolve(place ? { place, error: null } : { place: null, error: "not-named" });
            } catch {
                resolve({ place: null, error: "unreachable" });
            }
        }, (error) => resolve({
            place: null,
            error: error.code === 1 ? "denied" : "not-found",
        }), { timeout: 10000, maximumAge: 600000 });
    });
}
