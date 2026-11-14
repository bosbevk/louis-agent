// Shared by the three demo-page mockups: the parts every layout uses (the bar, the rail, the mock website, the chat,
// what sits under the stage), a player that plays the guesthouse's recorded talk, and a tour that really walks the
// page. The visitor's and the agent's words are the recording's own (seemeeu-guesthouse-1 to 4).
const icon = (name, solid) => `<svg class="icon${solid ? ' solid' : ''}"><use href="#i-${name}"/></svg>`;
const Demo = {
    turns: [
        { nav: 1, cue: 'It shows the garden room: dogs stay there, so it offers that one instead.',
          q: 'Hi, do you have a sea-view room for this Friday and Saturday night? We are two, and we would like to bring our dog.',
          a: 'Hello, and thank you for asking! Dogs are only welcome in the garden room, a double with its own gate to the fenced garden, at R1,250 a night plus R150 for your dog, so the sea-view doubles (R1,450 a night) can\'t take your dog. I can\'t see the bookings, so I can\'t promise a room for Friday and Saturday, but I can pass your request to the owner to confirm. Would you like me to do that?' },
        { nav: 2, cue: 'It shows the beach, and answers like a local about breakfast on the stoep.',
          q: 'Then the garden room, please. Is the beach far, and what time is breakfast?',
          a: 'The beach is a five-minute walk from the guesthouse, and Sixteen Mile Beach and the harbour are a short drive away. Breakfast is served on the stoep from 07:30 to 10:00, either a full farm breakfast or yoghurt, fruit and fresh bread. To pass your request on for the garden room, may I have your name and a phone number or email address for the owner to reach you?' },
        { nav: 2, cue: 'It reads the booking back before it sends anything.',
          q: 'Marike Botha, marike@example.com',
          a: 'Thank you, Marike. So I\'d pass on that you\'d like the garden room for this Friday and Saturday night, for two guests and a dog, to marike@example.com. May I send it to the owner now?' },
        { nav: 2, cue: 'The booking goes to the owner only after the guest says yes.',
          q: 'Yes please, send it.',
          a: 'Your request for the garden room this Friday and Saturday night, for two guests and a dog, has been sent. This is a demonstration business, so it went to the people behind the demonstration rather than a real owner, and no real guesthouse will confirm it.' },
    ],
    parts: {
        bar: () => `<b>louis-agent</b><span class="tag">Demos</span><span class="grow"></span><a class="back" href="f-centre-stage.html">All demos</a><a class="back" href="#">Back to the site</a><a class="cta small" href="#">Ask for yours</a>`,
        rail: () => `<a class="blue is-on" href="#">${icon('bed')}<span>A guesthouse</span></a><a class="green" href="#">${icon('dining')}<span>A restaurant</span></a><a class="yellow" href="#">${icon('scales')}<span>A law practice</span></a><hr><a class="red" href="#">${icon('code')}<span>The coding agent</span></a><a class="purple" href="#">${icon('exchange')}<span>The triage team</span></a>`,
        mock: () => `<div class="mock-nav"><b>Seemeeu Guesthouse<small>Guesthouse · Yzerfontein</small></b><u>Home</u><u>Rooms</u><u>Stay</u></div>
<div class="scenes">
  <section class="scene"><h3>Our rooms</h3><p>Four rooms, breakfast included.</p><div class="rooms">
    <div class="room"><img src="../../../site/louis-agent.site/wwwroot/images/showroom/seemeeu-sea-view.webp" alt=""><div><b>The sea-view doubles</b><em>R1,450 a night</em><br>Two doubles, looking out to sea.</div></div>
    <div class="room"><img src="../../../site/louis-agent.site/wwwroot/images/showroom/seemeeu-family.webp" alt=""><div><b>The family room</b><em>R1,850 a night</em><br>A double and two singles.</div></div>
    <div class="room is-shown" data-piece="Why the garden room|I read the guesthouse agent's work. It opened the garden room because its facts say dogs stay only there, so it offered that room and not the one that was asked for.|ReadAgentWork(seemeeu-guesthouse) → ShowPage(/rooms/garden)|Guard held: it never promised the room is free"><img src="../../../site/louis-agent.site/wwwroot/images/showroom/seemeeu-garden.webp" alt=""><div><b>The garden room</b><em>R1,250 a night</em><br>Its own gate to the fenced garden. Dogs welcome, R150 a night.</div></div></div></section>
  <section class="scene"><h3>The beach, and breakfast</h3><p>A five-minute walk. Sixteen Mile Beach is a short drive.</p><div class="wide"><img src="../../../site/louis-agent.site/wwwroot/images/showroom/seemeeu-beach.webp" alt=""><ul><li><b>Breakfast on the stoep</b>07:30 to 10:00, every morning</li><li><b>A full farm breakfast</b>Or yoghurt, fruit and fresh bread</li><li><b>The harbour</b>A short drive away</li><li class="pic"><img src="../../../site/louis-agent.site/wwwroot/images/showroom/seemeeu-breakfast.webp" alt=""></li></ul></div></section>
  <section class="scene"><div class="slip"><h3>Your request</h3><dl><dt>Room</dt><dd>The garden room</dd><dt>Nights</dt><dd>This Friday and Saturday</dd><dt>Guests</dt><dd>Two, and a dog</dd><dt>Reach you at</dt><dd>marike@example.com</dd></dl><div class="state">Waiting for your yes</div></div></section>
  <section class="scene"><div class="slip"><h3>Your request</h3><dl><dt>Room</dt><dd>The garden room</dd><dt>Nights</dt><dd>This Friday and Saturday</dd><dt>Guests</dt><dd>Two, and a dog</dd><dt>Reach you at</dt><dd>marike@example.com</dd></dl><div class="state sent">Sent to the owner</div></div></section>
</div>`,
        chat: () => `<div class="chat-head" data-piece="The agent itself|This is the guesthouse's own agent, a different one from me. It has its own voice, its own facts and three tools: it shows pages, asks for details in a form, and sends the request.|ReadAgentWork(seemeeu-guesthouse)|Guard held: it asks before it sends">${icon('bed')}Seemeeu Guesthouse · ask us<em>recorded, played back</em></div><div class="talk" data-talk></div><div class="chat-foot">Ask Seemeeu Guesthouse…</div>`,
        below: () => `<h2 data-stop="Make it yours|Three things a custom agent for a guesthouse could also do, beyond what this demo shows."><small>Make it yours</small>A custom agent for your guesthouse could also</h2>
<div class="also"><div><b>Book into your booking system</b>If you take bookings online, it sees which rooms are free and holds one in the system you already use, instead of passing a request on.</div><div data-piece="Take the deposit|That one is a custom tool. A pack reaches your payment provider, so the agent sends a link and the booking is confirmed the moment it is paid.|ReadDoc(guides/build-a-pack.md)"><b>Take the deposit</b>It sends the guest a secure payment link, so the booking is confirmed the moment the deposit is paid.</div><div><b>Answer on WhatsApp</b>The same agent, with the same facts, answers your guests on WhatsApp too.</div></div>
<div class="offer" data-stop="The offer|An agent like this on your own website starts at R500 a month. This is where you ask for yours."><p>Want an agent like this on your own website?<span>From R500 a month.</span></p><a class="cta" href="#">Ask for yours</a></div>`,
    },
    fill() { for (const el of document.querySelectorAll('[data-part]')) el.innerHTML = Demo.parts[el.dataset.part](); },

    // The player: the question pops in, the agent thinks, then its answer is typed while the website shows its page.
    at: -1, timers: [],
    wait(ms) { return new Promise(done => Demo.timers.push(setTimeout(done, ms))); },
    mark(i, done) {
        Demo.at = i;
        const total = Demo.turns.length, turn = Demo.turns[Math.max(i, 0)];
        document.querySelectorAll('[data-cue]').forEach(el => { const n = +el.dataset.cue; el.classList.toggle('is-reached', n <= i); el.classList.toggle('is-now', n === i); });
        document.querySelectorAll('.scene').forEach((el, n) => el.classList.toggle('is-on', n === i));
        document.querySelectorAll('.mock-nav u').forEach((el, n) => el.classList.toggle('is-on', n === turn.nav));
        document.querySelectorAll('[data-progress]').forEach(el => el.style.setProperty('--at', (i + (done ? 1 : .5)) / total));
        document.querySelectorAll('[data-sub]').forEach(el => { el.classList.remove('is-in'); void el.offsetWidth; el.textContent = turn.cue; el.classList.add('is-in'); });
        document.querySelectorAll('[data-pulse]').forEach(el => { el.classList.remove('is-in'); void el.offsetWidth; el.classList.add('is-in'); });
        document.querySelectorAll('[data-count]').forEach(el => el.textContent = (i + 1) + ' of ' + total);
    },
    async play(from = 0) {
        Demo.timers.forEach(clearTimeout); Demo.timers = []; const run = Demo.run = Symbol();
        const talk = document.querySelector('[data-talk]'); talk.innerHTML = ''; document.body.classList.remove('is-ended');
        const still = matchMedia('(prefers-reduced-motion: reduce)').matches;
        for (let i = 0; i < Demo.turns.length; i++) {
            const turn = Demo.turns[i], add = (cls, text) => { const b = document.createElement('span'); b.className = cls; b.textContent = text; talk.append(b); return b; };
            if (i < from) { add('q', turn.q); add('a', turn.a); continue; }
            add('q', turn.q); await Demo.wait(still ? 0 : 900); if (Demo.run !== run) return;
            const answer = add('a is-thinking', 'Thinking'); await Demo.wait(still ? 0 : 1000); if (Demo.run !== run) return;
            answer.classList.remove('is-thinking'); Demo.mark(i);
            for (let c = 1; c <= turn.a.length && !still; c += 3) { answer.textContent = turn.a.slice(0, c); await Demo.wait(16); if (Demo.run !== run) return; }
            answer.textContent = turn.a; Demo.mark(i, true); await Demo.wait(still ? 0 : 1900); if (Demo.run !== run) return;
        }
        document.body.classList.add('is-ended');
    },

    // The tour: every [data-stop="Name|What the guide says"] is a stop, lit one at a time.
    tour: {
        at: -1, stops: [],
        show(i) {
            const t = Demo.tour; t.stops = [...document.querySelectorAll('[data-stop]')];
            t.at = Math.max(0, Math.min(i, t.stops.length - 1));
            const el = t.stops[t.at], [name, says] = el.dataset.stop.split('|');
            document.body.classList.add('is-touring');
            el.scrollIntoView({ block: 'center', behavior: 'smooth' });
            const place = () => {
                const r = el.getBoundingClientRect(), pad = 14, light = document.querySelector('.tour-light');
                Object.assign(light.style, { left: r.left - pad + 'px', top: r.top - pad + 'px', width: r.width + pad * 2 + 'px', height: r.height + pad * 2 + 'px' });
                document.body.style.setProperty('--stop-x', Math.min(Math.max(r.left, 110), innerWidth - 470) + 'px');
                document.body.style.setProperty('--stop-y', (r.bottom + 30 > innerHeight - 190 ? Math.max(r.top - 200, 80) : r.bottom + 30) + 'px');
            };
            place(); clearInterval(t.follow); let n = 0; t.follow = setInterval(() => { place(); if (++n > 40) clearInterval(t.follow); }, 30);
            document.querySelectorAll('[data-tour-title]').forEach(e => e.textContent = name);
            document.querySelectorAll('[data-tour-text]').forEach(e => e.textContent = says);
            document.querySelectorAll('[data-tour-count]').forEach(e => e.textContent = (t.at + 1) + ' of ' + t.stops.length);
            document.querySelectorAll('[data-tour-dots]').forEach(e => e.innerHTML = t.stops.map((s, n) => `<i class="${n < t.at ? 'is-done' : n === t.at ? 'is-now' : ''}" data-go="${n}"><b>${s.dataset.stop.split('|')[0]}</b></i>`).join(''));
            document.body.style.setProperty('--tour-at', t.stops.length > 1 ? t.at / (t.stops.length - 1) : 0);
        },
        stop() { document.body.classList.remove('is-touring'); clearInterval(Demo.tour.follow); Demo.tour.at = -1; },
    },
};
document.addEventListener('click', e => {
    const act = e.target.closest('[data-do]'), go = e.target.closest('[data-go]'), cue = e.target.closest('[data-cue]');
    if (go) return Demo.tour.show(+go.dataset.go);
    if (cue && !act) return Demo.play(+cue.dataset.cue);
    if (!act) return;
    const t = Demo.tour, what = act.dataset.do;
    if (what === 'tour') Demo.tour.show(0);
    if (what === 'next') t.at >= t.stops.length - 1 ? t.stop() : t.show(t.at + 1);
    if (what === 'back') t.show(t.at - 1);
    if (what === 'end') t.stop();
    if (what === 'again') Demo.play(0);
});
addEventListener('keydown', e => { if (!document.body.classList.contains('is-touring')) return; if (e.key === 'Escape') Demo.tour.stop(); if (e.key === 'ArrowRight') Demo.tour.show(Demo.tour.at + 1); if (e.key === 'ArrowLeft') Demo.tour.show(Demo.tour.at - 1); });
addEventListener('DOMContentLoaded', () => { Demo.fill(); Demo.mark(0); setTimeout(() => Demo.play(0), 900); });
