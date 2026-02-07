function getUniqIdValue(prefix) {
    const p = (prefix && String(prefix).trim().length > 0) ? String(prefix).trim() : "id";
    if (window.crypto && typeof window.crypto.randomUUID === "function") {
        return p + "_" + window.crypto.randomUUID();
    }
    const s4 = () => Math.floor((1 + Math.random()) * 0x10000).toString(16).substring(1);
    return p + "_" + s4() + s4() + "_" + s4() + "_" + s4() + "_" + s4() + "_" + s4() + s4() + s4();
}