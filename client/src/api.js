// api.js
export class ApiError extends Error {
    constructor(message, { status, statusText, body } = {}) {
        super(message);
        this.name = "ApiError";
        this.status = status ?? 0;
        this.statusText = statusText ?? "";
        this.body = body;
    }
}

export function createApiClient({ baseUrl = "", fetchImpl = fetch } = {}) {
    async function request(path, { method = "GET", headers, body } = {}) {
        const res = await fetchImpl(baseUrl + path, { method, headers, body });

        const contentType = res.headers.get("content-type") || "";
        const isJson = contentType.includes("application/json");

        let payload = null;
        try {
            payload = isJson ? await res.json() : await res.text();
        } catch {
            // payload 保持 null 或 text，避免 JSON parse 炸掉遮蔽真正的 HTTP error
        }

        if (!res.ok) {
            const msg =
                (isJson && payload && (payload.error || payload.message)) ||
                `${res.status} ${res.statusText}` ||
                "Request failed";
            throw new ApiError(msg, {
                status: res.status,
                statusText: res.statusText,
                body: payload
            });
        }

        return payload;
    }

    return { request };
}
