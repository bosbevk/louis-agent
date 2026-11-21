// Passkeys (F61.5; a module since CL31): the browser's WebAuthn calls behind three functions the account pages import.
// The options come from the API as Identity wrote them, and the credential goes back as PublicKeyCredential.toJSON()
// writes it; no field is touched in between. A browser without the parsing helpers can't do passkeys, and the page
// says so.
export function supported() {
    return typeof navigator.credentials !== "undefined" &&
        typeof window.PublicKeyCredential !== "undefined" &&
        typeof window.PublicKeyCredential.parseCreationOptionsFromJSON === "function" &&
        typeof window.PublicKeyCredential.parseRequestOptionsFromJSON === "function";
}

// Makes a new passkey from the creation options; the person picks the authenticator in the browser's dialog.
export async function create(optionsJson) {
    const options = PublicKeyCredential.parseCreationOptionsFromJSON(JSON.parse(optionsJson));
    const credential = await navigator.credentials.create({ publicKey: options });
    return JSON.stringify(credential);
}

// Signs with an existing passkey from the request options.
export async function get(optionsJson) {
    const options = PublicKeyCredential.parseRequestOptionsFromJSON(JSON.parse(optionsJson));
    const credential = await navigator.credentials.get({ publicKey: options });
    return JSON.stringify(credential);
}
