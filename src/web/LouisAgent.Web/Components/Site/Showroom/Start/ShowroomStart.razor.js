// A showroom's start and the address bar (F134.1, a module beside its component, ADR 0093). Each demo's window has
// its own address (ADR 0107). When a visitor brings a demo forward, the bar shows that demo's address, so what they
// copy or send opens what they see. The entry is replaced, not added, so the back button still leaves the page.
export function show(address) {
    if (location.pathname !== address) history.replaceState(history.state, "", address);
}
