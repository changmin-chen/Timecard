// timecardApi.js (endpoint wrapper)
import { createApiClient } from "./api.js";

const client = createApiClient({ baseUrl: "" });

export const timecardApi = {
    getToday: () => client.request("/api/day/today"),

    punch: () => client.request("/api/punch", { method: "POST" }),

    deletePunch: (id) =>
        client.request(`/api/punches/${id}`, { method: "DELETE" }),

    addAttendanceRequest: (payload) =>
        client.request("/api/attendance-requests", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        }),

    deleteAttendanceRequest: (id) =>
        client.request(`/api/attendance-requests/${id}`, { method: "DELETE" }),

    setNonWorking: (date, payload) =>
        client.request(`/api/day/${date}/nonworking`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        }),

    getMonth: (y, m, includeEmpty) =>
        client.request(`/api/month/${y}/${m}?includeEmpty=${includeEmpty}`)
};
