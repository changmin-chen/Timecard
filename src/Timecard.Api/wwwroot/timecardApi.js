// timecardApi.js (endpoint wrapper)
import { createApiClient } from "./api.js";

const client = createApiClient({ baseUrl: "" }); // 同源，之後要換 server 再改

export const timecardApi = {
    getToday: () => client.request("/api/day/today"),
    clockIn: () => client.request("/api/clock/in", { method: "POST" }),
    clockOut: () => client.request("/api/clock/out", { method: "POST" }),

    deleteSession: (id) =>
        client.request(`/api/sessions/${id}`, { method: "DELETE" }),

    addAdjustment: (payload) =>
        client.request("/api/adjustments", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        }),

    deleteAdjustment: (id) =>
        client.request(`/api/adjustments/${id}`, { method: "DELETE" }),

    setNonWorking: (date, payload) =>
        client.request(`/api/day/${date}/nonworking`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        }),

    getMonth: (y, m, includeEmpty) =>
        client.request(`/api/month/${y}/${m}?includeEmpty=${includeEmpty}`)
};
