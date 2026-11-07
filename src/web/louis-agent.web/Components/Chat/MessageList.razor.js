// The message list's scrolling (CL31, beside MessageList): follows the stream only while the reader is already at
// the bottom, so scrolling up to read isn't interrupted.
export function scrollToBottom(element, force) {
    if (!element) return;
    const nearBottom = element.scrollHeight - element.scrollTop - element.clientHeight < 120;
    if (force || nearBottom) element.scrollTop = element.scrollHeight;
}
