// api.js
export class ApiError extends Error {
    constructor(message, { status, statusText, body, code, problemDetails } = {}) {
        super(message);
        this.name = "ApiError";
        this.status = status ?? 0;
        this.statusText = statusText ?? "";
        this.body = body;
        this.code = code ?? "";
        this.problemDetails = problemDetails ?? null;
    }
}

export function createApiClient({ baseUrl = "", fetchImpl = fetch } = {}) {
    async function request(path, { method = "GET", headers, body } = {}) {
        const res = await fetchImpl(baseUrl + path, { method, headers, body });

        const contentType = res.headers.get("content-type") || "";
        // Treat ProblemDetails as JSON too.
        const isJson =
            contentType.includes("application/json") ||
            contentType.includes("application/problem+json");

        let payload = null;
        let rawText = "";
        try {
            rawText = await res.text();
        } catch {
            rawText = "";
        }

        if (rawText) {
            // Fallback JSON detection: content-type can be missing or wrong.
            if (isJson || rawText.trim().startsWith("{") || rawText.trim().startsWith("[")) {
                try {
                    payload = JSON.parse(rawText);
                } catch {
                    payload = rawText;
                }
            } else {
                payload = rawText;
            }
        }

        if (!res.ok) {
            let msg = "Request failed";
            let code = "";
            let problemDetails = null;

            if (isJson && payload && typeof payload === "object") {
                problemDetails = payload;
                code = payload.extensions?.code || "";
                msg =
                    payload.detail ||
                    payload.title ||
                    payload.error ||
                    payload.message ||
                    `${res.status} ${res.statusText}` ||
                    msg;
            } else if (payload) {
                msg = payload;
            } else if (res.status || res.statusText) {
                msg = `${res.status} ${res.statusText}`;
            }

            throw new ApiError(msg, {
                status: res.status,
                statusText: res.statusText,
                body: payload,
                code,
                problemDetails
            });
        }

        return payload;
    }

    return { request };
}
