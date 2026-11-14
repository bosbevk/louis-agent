// The agent that runs the room, for mockups J and K. It is SIMULATED here: the real one is the site's guide, live.
// It behaves as the guide does today: one stop per answer, it lights the place with ShowPage, says a few lines, then
// draws its own choices with Offer. The mockup only changes where those show: on the stage, beside what it lights.
// A stop is any [data-stop="Name|What it says"]; a piece is any [data-piece="Name|What it says|The tool it used|A guard"].
const Agent = {
    total: 0, answers: 0, at: -1, run: null,
    wait: ms => new Promise(done => setTimeout(done, ms)),
    still: matchMedia('(prefers-reduced-motion: reduce)').matches,
    $: sel => document.querySelector(sel),
    stops: () => [...document.querySelectorAll('[data-stop]')],
    name: el => el.dataset.stop.split('|')[0],

    init() {
        const page = window.AgentPage;
        document.body.insertAdjacentHTML('beforeend', `
<div class="agent-light"></div>
<aside class="cap" aria-live="polite"><div class="cap-turn">
  <div class="cap-face">
    <div class="cap-who"><i></i><b>Louis · AI</b><span class="cap-cost"></span></div>
    <div class="cap-title"></div><div class="cap-says"></div><div class="cap-choices"></div>
    <button class="cap-ask" data-agent="ask">Or ask me anything, in your own words</button>
    <button class="cap-flip" data-agent="flip">How I did that</button>
  </div>
  <div class="cap-face back">
    <div class="cap-who"><i></i><b>How I did that</b><span>measured by the framework</span></div>
    <ul class="work-list"></ul><div class="work-sum"></div>
    <button class="cap-flip" data-agent="flip">Back to the answer</button>
  </div>
</div></aside>
<div class="agent" data-state="idle">
  <div class="agent-chips"></div>
  <div class="agent-bar">
    <span class="orb">L</span>
    <label class="agent-box"><span class="agent-doing">Louis · AI, the guide · type anything, any time</span><input type="text" autocomplete="off" placeholder="${page.placeholder}" aria-label="Ask the guide"></label>
    <button class="agent-cost" data-agent="drawer" title="The whole talk, and what it cost"><b>R0.00</b>0 answers</button>
    <button class="agent-send" data-agent="send" aria-label="Send">↑</button>
  </div>
  <span class="agent-note">The agent is simulated in this mockup. On the site it is the guide, live. Click any part with a dashed ring to ask about it.</span>
</div>
<aside class="talk-drawer"><h2>The talk so far<button data-agent="drawer" aria-label="Close">×</button></h2><p>Every answer, what it called and what it cost. The framework measures each request.</p><div class="talk-list"></div><div class="talk-total"><span>This talk</span><b>R0.00</b></div></aside>`);
        Agent.chips(page.chips);
        // The page keeps its foot clear of the bar: it is told how tall the bar stands, chips and all.
        const room = () => document.documentElement.style.setProperty('--agent-room', Agent.$('.agent').offsetHeight + 46 + 'px');
        new ResizeObserver(room).observe(Agent.$('.agent')); room();
        document.addEventListener('click', Agent.click);
        Agent.$('.agent-box input').addEventListener('keydown', e => { if (e.key === 'Enter') Agent.typed(); });
        addEventListener('keydown', e => { if (e.key === 'Escape') Agent.quiet(); });
        const keep = () => { if (Agent.$('.cap').classList.contains('is-on')) Agent.place(Agent.lit); };
        addEventListener('scroll', keep, { passive: true }); addEventListener('resize', keep);
    },
    chips(list) {
        Agent.idle = list;
        Agent.$('.agent-chips').innerHTML = list.map((c, i) => `<button class="${c.suggested ? 'is-suggested' : ''}" data-chip="${i}">${c.label}</button>`).join('');
    },
    state(state, doing) {
        const bar = Agent.$('.agent'); bar.dataset.state = state; bar.classList.toggle('is-busy', state !== 'idle' || Agent.$('.cap').classList.contains('is-on'));
        document.body.classList.toggle('agent-busy', state !== 'idle');
        Agent.$('.agent-doing').textContent = doing || 'Louis · AI, the guide · type anything, any time';
    },
    // Put the light on a place, and the caption beside it: to its right if there is room, else under or over it.
    place(el) {
        const light = Agent.$('.agent-light'), cap = Agent.$('.cap'), capH = cap.offsetHeight || 300, w = innerWidth, h = innerHeight - 150;
        if (!el) { light.classList.remove('is-on'); cap.style.setProperty('--cap-x', (w - 400) / 2 + 'px'); cap.style.setProperty('--cap-y', Math.max(90, h - capH - 40) + 'px'); return; }
        const r = el.getBoundingClientRect(), pad = 12;
        Object.assign(light.style, { left: r.left - pad + 'px', top: r.top - pad + 'px', width: r.width + pad * 2 + 'px', height: r.height + pad * 2 + 'px' });
        light.classList.add('is-on');
        let x, y;
        if (r.right + 36 + 400 < w) { x = r.right + 36; y = Math.min(Math.max(r.top, 80), h - capH); }
        else if (r.left - 36 - 400 > 0) { x = r.left - 436; y = Math.min(Math.max(r.top, 80), h - capH); }
        else if (r.bottom + 30 + capH < h) { x = Math.min(Math.max(r.left, 16), w - 416); y = r.bottom + 30; }
        else { x = Math.min(Math.max(r.right - 430, 16), w - 416); y = Math.max(r.top + 26, 80); }
        cap.style.setProperty('--cap-x', x + 'px'); cap.style.setProperty('--cap-y', Math.max(70, y) + 'px');
    },
    follow(el) { clearInterval(Agent.following); let n = 0; Agent.following = setInterval(() => { Agent.place(el); if (++n > 50) clearInterval(Agent.following); }, 30); },

    // One answer: think, call ShowPage (the light moves), say it (typed), then Offer the choices.
    async answer({ el, title, says, work = [], guard, choices = [], asked }) {
        const run = Agent.run = Symbol(), cap = Agent.$('.cap'), text = Agent.$('.cap-says');
        cap.classList.remove('is-work'); Agent.$('.cap-choices').innerHTML = '';
        // The guide holds whatever is playing while it talks about a part of it.
        if (el && window.AgentPage.hold) window.AgentPage.hold(el);
        if (asked) Agent.log(asked, null, true);
        Agent.state('thinking', 'Thinking…'); await Agent.wait(Agent.still ? 0 : 1000); if (Agent.run !== run) return;
        if (el) {
            Agent.state('showing', 'ShowPage → ' + title);
            if (el.dataset.swing !== undefined && typeof go === 'function') go(+el.dataset.swing);
            else el.scrollIntoView({ block: 'center', behavior: 'smooth' });
            await Agent.wait(Agent.still ? 0 : (el.dataset.swing !== undefined ? 950 : 500)); if (Agent.run !== run) return;
        }
        Agent.$('.cap-title').textContent = title || ''; Agent.$('.cap-title').hidden = !title; text.textContent = '';
        Agent.lit = el; Agent.place(el); Agent.follow(el); cap.classList.add('is-on');
        Agent.state('speaking', 'Speaking…'); text.classList.add('is-typing');
        for (let c = 2; c <= says.length && !Agent.still; c += 2) { text.textContent = says.slice(0, c); await Agent.wait(14); if (Agent.run !== run) return; }
        text.textContent = says; text.classList.remove('is-typing');
        // The work behind it: what it called, the guard that held, and what the framework measured.
        const cost = 0.016 + says.length * 0.00009, out = Math.round(says.length / 3.6) + 40, thought = (0.6 + says.length % 9 / 10).toFixed(1);
        Agent.total += cost; Agent.answers++;
        const calls = [...work, ...(choices.length ? [`Offer(${choices.length} choices)`] : [])];
        Agent.$('.work-list').innerHTML = calls.map(c => `<li>${c}</li>`).join('') + (guard ? `<li class="guard">${guard}</li>` : '');
        Agent.$('.work-sum').innerHTML = `<span><b>${thought}s</b>thinking</span><span><b>${out}</b>tokens out, 73k cached</span><span><b>R${cost.toFixed(3)}</b>this answer</span>`;
        Agent.$('.cap-cost').textContent = 'R' + cost.toFixed(3);
        Agent.log(says, `${calls.join(' · ') || 'no tool'} · ${thought}s · R${cost.toFixed(3)}`);
        Agent.choices = choices;
        Agent.$('.cap-choices').innerHTML = choices.map((c, i) => `<button class="${c.suggested ? 'is-suggested' : ''}" data-choice="${i}">${c.label}</button>`).join('');
        Agent.state('idle'); Agent.follow(el);
    },
    log(text, work, you) {
        Agent.$('.talk-list').insertAdjacentHTML('beforeend', `<article class="${you ? 'you' : ''}">${text}${work ? `<small>${work}</small>` : ''}</article>`);
        const total = 'R' + Agent.total.toFixed(2);
        Agent.$('.agent-cost').innerHTML = `<b>${total}</b>${Agent.answers} answer${Agent.answers === 1 ? '' : 's'}`; Agent.$('.talk-total b').textContent = total;
    },
    walk(i, asked) {
        const stops = Agent.stops(), el = stops[i]; if (!el) return Agent.bye(asked);
        Agent.at = i; const [title, says] = el.dataset.stop.split('|'), next = stops[i + 1], page = window.AgentPage;
        Agent.answer({ el, title, says, asked, work: [`ShowPage("${el.dataset.address || page.address + '/' + title.toLowerCase().replace(/[^a-z]+/g, '-')}")`], choices: [
            next ? { label: 'On to the next stop: ' + Agent.name(next), suggested: true, go: () => Agent.walk(i + 1, 'On to the next stop: ' + Agent.name(next)) } : { label: 'That was the last stop', suggested: true, go: () => Agent.bye('That was the last stop') },
            { label: 'Explain another part', go: () => Agent.pick() },
            ...(page.live ? [{ label: 'Ask it live', go: () => Agent.live() }] : []),
            ...(el.dataset.open ? [{ label: el.dataset.open, go: () => location.href = 'k-the-agent-runs-the-demo.html' }] : []),
            { label: 'Stop here', go: () => Agent.bye('Stop here') }] });
    },
    pick() { Agent.answer({ asked: 'Explain another part', says: 'Sure. Which part shall I show you?', choices: Agent.stops().map((s, i) => ({ label: Agent.name(s), go: () => Agent.walk(i, Agent.name(s)) })) }); },
    live() { Agent.answer({ asked: 'Ask it live', el: Agent.$('[data-live]'), title: 'Ask it live', says: 'Here it is. Type your own question to the guesthouse\'s agent, and it answers live from its own facts. I\'ll wait here.', work: ['ShowPage("/guesthouse-demo/the-demo")', 'AskAgent("seemeeu-guesthouse") is ready'], choices: [{ label: 'Carry on with the tour', suggested: true, go: () => Agent.walk(Agent.at + 1, 'Carry on with the tour') }, { label: 'Stop here', go: () => Agent.bye('Stop here') }] }); },
    async bye(asked) { await Agent.answer({ asked, says: 'Good. I\'m down here whenever you want me.' }); setTimeout(() => Agent.quiet(), 2600); },
    quiet() { Agent.run = null; Agent.$('.cap').classList.remove('is-on'); Agent.$('.agent-light').classList.remove('is-on'); Agent.state('idle'); },
    piece(el) {
        const [title, says, tool, guard] = el.dataset.piece.split('|');
        Agent.answer({ el, title, says, asked: 'Tell me about: ' + title, guard, work: tool ? [tool] : [], choices: [{ label: Agent.at < 0 ? 'Walk me through this page' : 'Carry on with the tour', suggested: true, go: () => Agent.walk(Agent.at < 0 ? 0 : Agent.at + 1, 'Carry on') }, { label: 'Stop here', go: () => Agent.bye('Stop here') }] });
    },
    typed() {
        const input = Agent.$('.agent-box input'), asked = input.value.trim(); if (!asked) return; input.value = '';
        Agent.answer({ asked, says: 'I\'m simulated in this mockup, so I can\'t answer that one properly. On the site I would read the docs and the page you are on, answer in a few lines, and show you the place it is about.', work: ['FindPlace("' + asked.slice(0, 28) + '")', 'ReadDoc(…)'], choices: [{ label: 'Walk me through this page', suggested: true, go: () => Agent.walk(0, 'Walk me through this page') }, { label: 'Stop here', go: () => Agent.bye('Stop here') }] });
    },
    click(e) {
        const chip = e.target.closest('[data-chip]'), choice = e.target.closest('[data-choice]'), act = e.target.closest('[data-agent]'), piece = e.target.closest('[data-piece]');
        if (chip) return Agent.idle[+chip.dataset.chip].go();
        if (choice) return Agent.choices[+choice.dataset.choice].go();
        if (act) { const what = act.dataset.agent; if (what === 'flip') Agent.$('.cap').classList.toggle('is-work'); if (what === 'drawer') Agent.$('.talk-drawer').classList.toggle('is-open'); if (what === 'send') Agent.typed(); if (what === 'ask') { const box = Agent.$('.agent-box input'); box.focus(); Agent.$('.agent-bar').classList.remove('is-called'); void box.offsetWidth; Agent.$('.agent-bar').classList.add('is-called'); } return; }
        if (piece && !e.target.closest('a, button')) { e.stopPropagation(); Agent.piece(piece); }
    },
};
addEventListener('DOMContentLoaded', () => setTimeout(Agent.init, 50));
