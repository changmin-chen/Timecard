# Timecard MVP (.NET + PostgreSQL + EF Core + Vue 3)

這是一個「個人用」的工時管理系統 MVP：
- 記錄上下班（支援多段 session）
- 手動加「調整」（請假 / 出差 / 假日 / 颱風假等）
- 日/月報表：每天 9 小時（含午休），每日彈性時數最多累積/使用 55 分鐘，每月重置

## 用 Docker 部署（推薦）

需求：Docker 與 Docker Compose

```bash
# 啟動（首次會自動 build image）
docker compose up -d

# 查看 log
docker compose logs -f timecard

# 停止
docker compose down
```

App 預設監聽 `http://127.0.0.1:8080`（僅內網）。

可透過環境變數或 `.env` 檔調整：

| 變數 | 預設值 | 說明 |
|------|--------|------|
| `POSTGRES_PASSWORD` | `change_me_strong_password` | DB 密碼，**建議修改** |
| `TIMECARD_HTTP_PORT` | `8080` | App 對外埠號 |
| `POSTGRES_HOST_PORT` | `15432` | PostgreSQL 對外埠號 |

EF Core migration 會在 App 啟動時自動執行。

## 本機開發

需求：.NET 10 SDK、Node.js 22+、PostgreSQL

```bash
# 後端
dotnet run --project src/Timecard.Api/Timecard.Api.csproj

# 前端（開發模式，proxy 到後端）
cd client && npm install && npm run dev
```

資料庫連線字串：`ConnectionStrings:Timecard`（見 `appsettings.json` 或 user secrets）

## API 快速測試

```bash
curl -X POST http://localhost:8080/api/clock/in   # 打卡上班
curl -X POST http://localhost:8080/api/clock/out  # 打卡下班
curl http://localhost:8080/api/day/today           # 今天摘要
curl http://localhost:8080/api/month/2026/2        # 本月報表
```
