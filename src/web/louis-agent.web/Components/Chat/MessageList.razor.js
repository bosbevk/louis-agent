// The message list's scrolling (CL31, beside MessageList): follows the stream to its end until the reader scrolls up
// themselves, so reading back isn't interrupted. The rule used to be "follow while within 120px of the bottom", and an
// answer that arrived a paragraph at a time left the list more than that behind after one draw, so it stopped
// following for the rest of the turn (FX101, Louis: "my scroll chat did not go to the end when the turn ended").
// Whether to follow is now the reader's: a scroll they make up leaves it, one back to the bottom takes it up again,
// and a new turn (force) always follows.
const following = new WeakMap();

// Scrolls the list to its end when it is following, or when told to.
export function scrollToBottom(element, force) {
    if (!element) return;
    watch(element);
    if (force) following.set(element, true);
    if (following.get(element) === false) return;
    element.dataset.ownScroll = "1";
    element.scrollTop = element.scrollHeight;
}

// Hears the reader's own scrolls, once per list: the one this module makes is skipped, any other sets whether to follow.
const watch = (element) => {
    if (element.dataset.watched) return;
    element.dataset.watched = "1";
    element.addEventListener("scroll", () => {
        if (element.dataset.ownScroll) {
            delete element.dataset.ownScroll;
            return;
        }
        following.set(element, element.scrollHeight - element.scrollTop - element.clientHeight < 40);
    }, { passive: true });
};
