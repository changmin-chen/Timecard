# API 參考

將 `<host>` 替換為實際部署位址（本機開發為 `https://localhost:49177`）。

---

## 認證

### `POST /api/auth/login`
登入。

```json
{ "email": "user@example.com", "password": "secret" }
```

### `POST /api/auth/logout`
登出。

### `GET /api/auth/me`
取得目前登入的使用者資訊。

### `POST /api/auth/change-password`
更改密碼。

```json
{ "currentPassword": "old", "newPassword": "new" }
```

---

## 打卡 & 日報

### `GET /api/day/today`
取得今天的出勤資料（台灣時間）。

### `GET /api/day/{date}`
取得指定日期的出勤資料。`date` 格式：`yyyy-MM-dd`。

### `POST /api/punch`
新增打卡紀錄（通常由外部系統呼叫，不建議手動使用）。

### `DELETE /api/punches/{id}`
刪除指定打卡紀錄。

---

## 出缺勤調整

### `POST /api/attendance-requests`
新增出缺勤申請（請假、加班等）。

```json
{
  "date": "2026-03-15",
  "category": "Leave",
  "startTime": "09:00",
  "endTime": "18:00",
  "note": "事假"
}
```

### `PUT /api/attendance-requests/{id}`
修改出缺勤申請。

### `DELETE /api/attendance-requests/{id}`
刪除出缺勤申請。

---

## 月報

### `GET /api/month/{year}/{month}`
取得整月報表，包含每日工時、彈性增減、不足分鐘。

```bash
curl http://<host>/api/month/2026/3
# 支援 ?includeEmpty=true 顯示無紀錄日
```

---

## 外部打卡同步

### `POST /api/sync-punch/punches`
從外部系統（如門禁機）匯入打卡資料。需帶 API Key（`X-Api-Key` header）。

---

## 行事曆

### `GET /api/calendar/{date}`
查詢指定日期的行事曆資訊（工作日／假日、備註）。

### `POST /api/calendar/import/tw-dgpa` *(Admin)*
匯入台灣政府人事行政局行事曆（CSV 格式，multipart form data）。

---

## 管理員

### `GET /api/admin/users` *(Admin)*
列出所有使用者。

### `POST /api/admin/users` *(Admin)*
新增使用者帳號。

```json
{ "email": "newuser@example.com", "employeeId": "A001", "displayName": "王小明", "password": "temp" }
```

### `POST /api/admin/users/{id}/reset-password` *(Admin)*
重設使用者密碼，回傳臨時密碼。

### `GET /api/admin/export/month/{year}/{month}` *(Admin)*
匯出指定月份所有使用者的出勤 CSV。
