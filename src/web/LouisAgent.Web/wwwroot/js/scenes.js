// The product windows' scenes, played (FX163, the design's window): each scene's steps are written in its markup,
// and this plays one scene, then the next, in a loop. The frame brings it to every page, as it does the lens, and
// starts every window it finds after each render. The visitor's and the agent's words are typed out, a thought stands
// a moment, a call spins and then stands done, and a step may move the page on (the right card shown, the request
// drawn, sent) and change the address, the line under the chat and the request's status. A visitor who asked for less
// motion sees each line whole at once.
const wait = ms => new Promise(done => setTimeout(done, ms));
const playing = new WeakMap();
const STAGES = ["is-shown", "is-card", "is-sent"];

// The scenes a window holds, each with what its steps change and how it started, so a scene starts afresh each time.
const scenesOf = root => [...root.querySelectorAll(".product-scene")].map(scene => {
    const talk = scene.querySelector("[data-talk]"), address = scene.querySelector("[data-address]");
    const foot = scene.querySelector("[data-foot]"), status = scene.querySelector("[data-status]");
    const steps = [...scene.querySelectorAll(".product-script > i")].map(i => ({ ...i.dataset, text: i.textContent }));
    return { scene, talk, address, foot, status, steps,
        start: { address: address.textContent, foot: foot.textContent, status: status?.textContent } };
});

// Plays one window's scenes until it is stopped.
function play(root) {
    if (!root || playing.has(root)) return;
    const state = { on: true };
    playing.set(root, state);
    const still = matchMedia("(prefers-reduced-motion: reduce)").matches;
    const scenes = scenesOf(root);
    const add = (talk, kind, text) => {
        const line = document.createElement("div");
        line.className = `product-${kind}`;
        line.textContent = text;
        talk.append(line);
        return line;
    };
    const type = async (line, text) => {
        if (still) { line.textContent = text; return; }
        for (let i = 2; i <= text.length && state.on; i += 2) { line.textContent = text.slice(0, i); await wait(18); }
        line.textContent = text;
    };
    const step = async (s, at) => {
        const { talk } = s;
        if (at.kind === "ask") await type(add(talk, "ask", ""), at.text);
        if (at.kind === "answer") await type(add(talk, "answer", ""), at.text);
        if (at.kind === "think") { const thought = add(talk, "think", at.text); await wait(+at.ms || 900); thought.remove(); }
        if (at.kind === "call") { const call = add(talk, "call is-running", at.text); await wait(650); call.classList.remove("is-running"); }
        if (at.kind === "offer") {
            const offer = add(talk, "offer", "");
            at.text.split("|").forEach(choice => { const pill = document.createElement("span"); pill.textContent = choice; offer.append(pill); });
        }
        if (at.kind === "press") talk.querySelector(".product-offer span")?.classList.add("is-pressed");
        if (at.kind === "wait") await wait(+at.ms || 1000);
        if (at.stage) s.scene.classList.add(`is-${at.stage}`);
        if (at.address) s.address.textContent = at.address;
        if (at.foot) s.foot.textContent = at.foot;
        if (at.status && s.status) s.status.textContent = at.status;
    };
    (async () => {
        for (let n = 0; state.on; n = (n + 1) % scenes.length) {
            const s = scenes[n];
            scenes.forEach(other => other.scene.classList.toggle("is-on", other === s));
            s.talk.replaceChildren();
            s.scene.classList.remove(...STAGES);
            s.address.textContent = s.start.address;
            s.foot.textContent = s.start.foot;
            if (s.status) s.status.textContent = s.start.status;
            await wait(900);
            for (const at of s.steps) { if (!state.on) return; await step(s, at); }
            await wait(still ? 60000 : 4600);
        }
    })();
}

// Stops one window's playing.
function stop(root) {
    const state = playing.get(root);
    if (state) state.on = false;
    playing.delete(root);
}

// The windows on the page: each new one starts playing, each one gone stops. Safe to call after every render.
const started = new Set();
export const scenes = {
    attach() {
        [...started].forEach(root => { if (!root.isConnected) { stop(root); started.delete(root); } });
        document.querySelectorAll(".product-window").forEach(root => { if (!started.has(root)) { started.add(root); play(root); } });
    },
    detach() {
        started.forEach(stop);
        started.clear();
    }
};
