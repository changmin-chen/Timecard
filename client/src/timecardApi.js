// timecardApi.js (endpoint wrapper)
import { createApiClient } from "./api.js";

const client = createApiClient({ baseUrl: "" });

export const timecardApi = {
    login: (email, password) =>
        client.request("/api/auth/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email, password }),
        }),

    changePassword: (currentPassword, newPassword) =>
        client.request("/api/auth/change-password", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ currentPassword, newPassword }),
        }),

    createUser: (email, employeeId, displayName, temporaryPassword) =>
        client.request("/api/admin/users", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email, employeeId, displayName, temporaryPassword }),
        }),

    listUsers: () => client.request("/api/admin/users"),

    resetUserPassword: (id) =>
        client.request(`/api/admin/users/${id}/reset-password`, {
            method: "POST",
        }),

    getToday: () => client.request("/api/day/today"),

    getDay: (date) => client.request(`/api/day/${date}`),

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

    getMonth: (y, m, includeEmpty) =>
        client.request(`/api/month/${y}/${m}?includeEmpty=${includeEmpty}`),

    importCalendarCsv: (file) => {
        const form = new FormData()
        form.append('file', file)
        return client.request('/api/calendar/import/tw-dgpa', { method: 'POST', body: form })
    },

    monthExportUrl: (year, month) => `/api/admin/export/month/${year}/${month}`,
};
