// The lens (F151.6, the design's real glass, Louis's way 2 of three): in a theme that has one, the pane under the
// pointer, and the one picked or shown, bends what is behind it as a piece of glass does, and its edge catches the
// light. Every other pane stays clear glass, so the page pays for one or two lenses, never for all of them.
//
// A pane says what it is in its own styles (--web-lens-piece, from the surface tool): 1 for a piece the pointer may
// light, 2 for one picked, 3 for one the agent shows. The theme says how thick the glass is (--web-lens-wall at its
// edge, --web-lens-rise for how much higher its middle stands); a wall of 0 is no lens. The frame attaches it after
// every render. Only Chromium runs an SVG filter on what is behind a piece, so every other browser keeps the clear
// pane and nothing breaks.
//
// From one shape come both things glass does to light. Its top rises from the edge to the middle as a squircle;
// what lies under it is moved by Snell's law, the rule for how light turns as it goes from air into glass; and it is
// lit by where its top faces, a lamp high on the left, its answer low on the right, and the bright line every steep
// edge of glass carries. The pictures are worked out once for each size and kept, the bending at half size.
const NS = "http://www.w3.org/2000/svg";
const INDEX = 1.7;     // how much slower light is in this glass than in air; window glass is about 1.5
const STEEPEST = 8;    // a top is never taken as steeper than this, so the very edge stays a number
const NEAR = "400px";  // a picked piece wears its lens only this close to the screen
const KEY = [-.42, -.5, .76], BACK = [.42, .5, .76];   // half-way between the eye and the lamp, and its answer
// Under the pointer the lamp is the pointer, a hand above the piece, so the shine and the shade walk round the edge as
// it moves (FX163, the design's lens): how much stronger it is on the edge than the far lamp, how far the glass must
// turn from flat before it shows in full, so the flat of a piece takes none, and how high it hangs, in pixels.
const LIT = 1.35, EDGE = .12, HAND = 70;
// A piece held down is this much of its height: the glass gives, so it bends less and its shine draws in.
const PRESSED = .45;
// Light let in from inside: white for a picked piece, violet for one the agent shows.
const GLOW = { 2: [255, 255, 255, .8], 3: [150, 70, 255, .9] };
const shape = t => Math.pow(1 - Math.pow(1 - t, 4), .25);
const climb = t => Math.pow(1 - t, 3) / Math.pow(Math.max(1 - Math.pow(1 - t, 4), 1e-5), .75);
const shift = (high, slope) => {
    const a = Math.atan(slope), b = Math.asin(Math.sin(a) / INDEX);
    return high * Math.tan(a - b);
};

// One side of a piece, n pixels long at `fine` samples a pixel: how high its top stands there, and how fast it climbs.
const side = (n, fine, half, rise) => {
    const m = Math.max(1, Math.round(n * fine)), F = new Float32Array(m), D = new Float32Array(m);
    for (let i = 0; i < m; i++) {
        const p = (i + .5) / fine, t = Math.min(Math.min(p, n - p) / half, 1);
        F[i] = shape(t); D[i] = (p < n / 2 ? 1 : -1) * climb(t) * rise / half;
    }
    return [F, D, m];
};
const picture = (W, H, paint) => {
    const c = document.createElement("canvas"); c.width = W; c.height = H;
    const g = c.getContext("2d"), im = g.createImageData(W, H); paint(im.data); g.putImageData(im, 0, 0);
    return c.toDataURL();
};

// The bending, as a picture: red says how far to reach sideways, green how far up or down, mid grey is "stay".
const bendMap = (w, h, wall, rise) => {
    const half = Math.min(w, h) / 2, [FX, DX, W] = side(w, .5, half, rise), [FY, DY, H] = side(h, .5, half, rise);
    const sx = new Float32Array(W * H), sy = new Float32Array(W * H); let far = 0;
    for (let y = 0, i = 0; y < H; y++) for (let x = 0; x < W; x++, i++) {
        const gx = DX[x] * FY[y], gy = DY[y] * FX[x], l = Math.hypot(gx, gy); if (!l) continue;
        const s = shift(wall + rise * FX[x] * FY[y], Math.min(l, STEEPEST));
        sx[i] = gx / l * s; sy[i] = gy / l * s; far = Math.max(far, s);
    }
    const href = picture(W, H, d => { for (let i = 0, j = 0; i < W * H; i++, j += 4) {
        d[j] = 128 + sx[i] / (far || 1) * 127; d[j + 1] = 128 + sy[i] / (far || 1) * 127;
        d[j + 2] = 128; d[j + 3] = 255;
    } });
    return [href, far];
};

// The far lamp, high on the left, and its answer low on the right: the shine, its echo, and which way is away.
const fromAfar = (nx, ny, nz) => [
    Math.pow(Math.max(nx * KEY[0] + ny * KEY[1] + nz * KEY[2], 0), 60),
    Math.pow(Math.max(nx * BACK[0] + ny * BACK[1] + nz * BACK[2], 0), 60),
    nx * .64 + ny * .77];
// The pointer's lamp: the way from this point to it, half way between that and straight up to the eye, so the edge
// shines where it faces that way and is in shade where it turns from it. Only the edge takes it.
const fromThePointer = (lamp, x, y, nx, ny, nz) => {
    const lx = lamp.x - x, ly = lamp.y - y, ll = Math.hypot(lx, ly, HAND);
    const hx = lx / ll, hy = ly / ll, hz = HAND / ll + 1, hl = Math.hypot(hx, hy, hz), flat = Math.hypot(lx, ly) || 1;
    const edge = Math.min((1 - nz) / EDGE, 1);
    return [
        LIT * 1.3 * edge * Math.pow(Math.max((nx * hx + ny * hy + nz * hz) / hl, 0), 36),
        LIT * edge * Math.pow(Math.max((-nx * hx - ny * hy + nz * hz) / hl, 0), 36),
        -(nx * lx + ny * ly) / flat];
};

// The light on the piece, laid over the bent page: a shine where its top faces half way to the lamp, a weaker one
// from the far side, the edge mirroring the room, shade on the steep side away from the lamp, and for a picked or a
// shown piece its whole edge glowing from inside.
const lightMap = (w, h, rise, dark, glow, lamp) => {
    const half = Math.min(w, h) / 2, [FX, DX, W] = side(w, 1, half, rise), [FY, DY, H] = side(h, 1, half, rise);
    const ink = dark ? [0, 0, 0] : [42, 24, 150], deep = dark ? .5 : .3;
    return picture(W, H, d => { for (let y = 0, j = 0; y < H; y++) for (let x = 0; x < W; x++, j += 4) {
        let gx = DX[x] * FY[y], gy = DY[y] * FX[x]; const steep = Math.hypot(gx, gy);
        if (steep > STEEPEST) { gx *= STEEPEST / steep; gy *= STEEPEST / steep; }
        const l = Math.hypot(gx, gy, 1), nx = -gx / l, ny = -gy / l, nz = 1 / l;
        const [key, back, away] = lamp ? fromThePointer(lamp, x, y, nx, ny, nz) : fromAfar(nx, ny, nz);
        const mirror = (.04 + .96 * Math.pow(1 - nz, 5)) * (.25 + .75 * Math.max(-away, 0));
        const white = Math.min(key + .5 * back + (lamp ? LIT : 1) * .9 * mirror, 1);
        const shade = deep * Math.pow(1 - nz, 1.5) * Math.max(away, 0) * (1 - white);
        const inside = glow ? Math.min(glow[3] * Math.pow(1 - nz, .8), 1) * (1 - white) : 0, a = white + shade + inside;
        if (a < .004) continue;
        for (let k = 0; k < 3; k++) d[j + k] = (255 * white + ink[k] * shade + (glow ? glow[k] * inside : 0)) / a;
        d[j + 3] = Math.min(a, 1) * 255;
    } });
};
const kept = new Map();
const memo = (key, make) => {
    if (!kept.has(key)) { if (kept.size > 60) kept.clear(); kept.set(key, make()); }
    return kept.get(key);
};

let glass = { wall: 0, rise: 0 }, host = null, n = 0, hovered = null, listening = false;
let near = null, sized = null, looks = null;
const worn = new Map();   // each piece wearing a lens, and its filter's id
const read = () => { const s = getComputedStyle(document.documentElement);
    glass = { wall: +s.getPropertyValue("--web-lens-wall") || 0, rise: +s.getPropertyValue("--web-lens-rise") || 0 }; };
// A panel a site switched the lens off on (data-lens="off") is no piece of glass to it.
const kind = el => el.dataset?.lens === "off" ? 0 : +getComputedStyle(el).getPropertyValue("--web-lens-piece") || 0;

// Puts the lens on a piece, or draws it again at its new size or light.
// What a piece is going through just now: where the pointer's lamp is over it, and whether it is held down.
const now = new Map();
const state = el => { if (!now.has(el)) now.set(el, {}); return now.get(el); };
const dark = () => document.documentElement.dataset.style !== "light";
// The light's picture for a piece as it is now: with the pointer's lamp it is drawn fresh, else it is kept by size.
const litFor = (el, w, h, rise) => {
    const m = state(el), k = kind(el);
    return m.lamp ? lightMap(w, h, rise, dark(), GLOW[k], m.lamp)
        : memo(`l${w}x${h}:${rise}:${dark()}:${k}`, () => lightMap(w, h, rise, dark(), GLOW[k]));
};
const wear = el => {
    const w = Math.round(el.offsetWidth), h = Math.round(el.offsetHeight); if (!w || !h || !glass.wall) return;
    // A piece held down gives: its wall a little thinner and its middle much lower, so it bends less.
    const down = state(el).down, wall = glass.wall * (down ? .9 : 1), rise = glass.rise * (down ? PRESSED : 1);
    const id = worn.get(el) ?? `lens-${n++}`; worn.set(el, id); sized?.observe(el);
    let f = document.getElementById(id);
    if (!f) { f = document.createElementNS(NS, "filter"); f.id = id; host.append(f); }
    ["color-interpolation-filters:sRGB", "filterUnits:userSpaceOnUse", "primitiveUnits:userSpaceOnUse",
        "x:0", "y:0", `width:${w}`, `height:${h}`]
        .forEach(pair => { const [name, value] = pair.split(":"); f.setAttribute(name, value); });
    const [bend, far] = memo(`b${w}x${h}:${wall}:${rise}`, () => bendMap(w, h, wall, rise));
    const lit = litFor(el, w, h, rise);
    const box = `x="0" y="0" width="${w}" height="${h}" preserveAspectRatio="none"`;
    f.innerHTML = `<feImage ${box} result="m" href="${bend}"/>` +
        `<feDisplacementMap in="SourceGraphic" in2="m" scale="${far * 2}" xChannelSelector="R" yChannelSelector="G"` +
        ` result="bent"/>` +
        `<feImage ${box} result="lit" href="${lit}"/><feBlend in="lit" in2="bent" mode="normal"/>`;
    // The pane's own deepening of the page stays, after the bending, but never a blur: bending only shows when what is
    // behind stays sharp, as the design's.
    el.style.removeProperty("backdrop-filter");
    const own = getComputedStyle(el).backdropFilter.replace(/blur\([^)]*\)/g, "").trim();
    const value = `url(#${id})${own && own !== "none" ? " " + own : ""}`;
    el.style.setProperty("-webkit-backdrop-filter", value); el.style.setProperty("backdrop-filter", value);
};
const unwear = el => {
    const id = worn.get(el); if (!id) return;
    worn.delete(el); now.delete(el); sized?.unobserve(el); document.getElementById(id)?.remove();
    el.style.removeProperty("-webkit-backdrop-filter"); el.style.removeProperty("backdrop-filter");
};
const held = el => el.isConnected && kind(el) >= 2;

// The pointer: the nearest piece round what it is over wears the lens, and the one it left takes it off, unless
// that one is picked or shown.
const over = e => {
    if (!glass.wall) return;
    let el = e.target instanceof Element ? e.target : null;
    while (el && el !== document.body && !kind(el)) el = el.parentElement;
    const piece = el && el !== document.body ? el : null; if (piece === hovered) return;
    if (hovered && !held(hovered)) unwear(hovered);
    hovered = piece; if (piece) wear(piece);
};
const left = e => { if (e.relatedTarget || !hovered) return; if (!held(hovered)) unwear(hovered); hovered = null; };

// Only the light changes while the pointer moves, so only the light's picture is swapped, at most once a frame.
const relight = el => {
    const lit = document.getElementById(worn.get(el) ?? "")?.querySelector('[result="lit"]'); if (!lit) return;
    const w = Math.round(el.offsetWidth), h = Math.round(el.offsetHeight), down = state(el).down;
    lit.setAttribute("href", litFor(el, w, h, glass.rise * (down ? PRESSED : 1)));
};
let frame = 0;
// The pointer over the piece is its lamp: where it is, in the piece's own pixels, even on a piece turned in 3D.
const moved = e => {
    if (!hovered || !worn.has(hovered)) return;
    const r = hovered.getBoundingClientRect(); if (!r.width || !r.height) return;
    state(hovered).lamp = { x: (e.clientX - r.left) * hovered.offsetWidth / r.width,
        y: (e.clientY - r.top) * hovered.offsetHeight / r.height };
    const el = hovered; if (!frame) frame = requestAnimationFrame(() => { frame = 0; relight(el); });
};
// Holding the piece down flattens it; letting go stands it up again.
const pressed = () => { if (!hovered || !worn.has(hovered)) return; state(hovered).down = true; wear(hovered); };
const released = () => {
    [...now.entries()].filter(([, m]) => m.down).forEach(([el, m]) => { m.down = false; if (worn.has(el)) wear(el); });
};

// The picked and shown pieces, looked for after a render, at most a few times a second: each wears the lens while
// it is near the screen, and one no longer picked gives it back.
let looking = 0;
const look = () => { if (looking) return; looking = setTimeout(() => { looking = 0;
    [...worn.keys()].forEach(el => { if (el !== hovered && !held(el)) { unwear(el); near.unobserve(el); } });
    if (!glass.wall) return;
    document.querySelectorAll(".site *").forEach(el => { if (kind(el) >= 2) near.observe(el); });
}, 200); };

// Hangs the listeners once and looks for what the latest render picked; safe to call after every render.
export const lens = {
    attach() {
        if (!navigator.userAgentData?.brands?.some(b => b.brand === "Chromium")) return;
        if (!listening) {
            listening = true; read();
            host = document.createElementNS(NS, "svg"); host.setAttribute("width", 0); host.setAttribute("height", 0);
            host.setAttribute("aria-hidden", "true"); host.style.position = "absolute"; document.body.append(host);
            document.addEventListener("pointerover", over, { passive: true });
            document.addEventListener("pointerout", left, { passive: true });
            document.addEventListener("pointermove", moved, { passive: true });
            document.addEventListener("pointerdown", pressed, { passive: true });
            document.addEventListener("pointerup", released, { passive: true });
            near = new IntersectionObserver(es => es.forEach(e => e.isIntersecting && held(e.target) ? wear(e.target)
                : e.target !== hovered && unwear(e.target)), { rootMargin: `${NEAR} 0px` });
            sized = new ResizeObserver(es => es.forEach(e => worn.has(e.target) && wear(e.target)));
            // A new theme or style: read the glass again and draw every lens anew, or take them off.
            looks = new MutationObserver(() => {
                read();
                [...worn.keys()].forEach(el => glass.wall ? wear(el) : unwear(el));
                look();
            });
            const named = { attributes: true, attributeFilter: ["data-theme", "data-style"] };
            looks.observe(document.documentElement, named);
        }
        look();
    },
    detach() {
        if (!listening) return;
        listening = false;
        document.removeEventListener("pointerover", over); document.removeEventListener("pointerout", left);
        document.removeEventListener("pointermove", moved); document.removeEventListener("pointerdown", pressed);
        document.removeEventListener("pointerup", released);
        [near, sized, looks].forEach(o => o?.disconnect());
        [...worn.keys()].forEach(unwear); host?.remove(); host = null; hovered = null;
    }
};
