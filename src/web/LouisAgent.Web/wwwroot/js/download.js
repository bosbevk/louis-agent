// A file the page fetched, handed to the browser as a download (F18 step 6a.3; the framework's since CL31): the bytes
// come base64-encoded from the component, and the browser saves them under the name given.
export function download(name, base64, type) {
    const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    const url = URL.createObjectURL(new Blob([bytes], { type }));
    const link = document.createElement("a");
    link.href = url;
    link.download = name;
    link.click();
    URL.revokeObjectURL(url);
}
