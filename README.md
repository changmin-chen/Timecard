# Timecard MVP (.NET + PostgreSQL + EF Core + Vue 3)

這是一個「個人用」的工時管理系統 MVP：
- 記錄上下班（支援多段 session）
- 手動加「調整」（請假 / 出差 / 假日 / 颱風假等）
- 日/月報表：每天 9 小時（含午休），每日彈性時數最多累積/使用 55 分鐘，每月重置

## 用 Docker 部署（推薦）

需求：Docker 與 Docker Compose

```bash
# 複製範本並填入實際值
cp .env.example .env

# 啟動（首次會自動 build image）
docker compose up -d

# 查看 log
docker compose logs -f timecard

# 停止
docker compose down
```

App 監聽 `http://192.168.1.44:49178`。

EF Core migration 會在 App 啟動時自動執行。

### 環境變數（`.env`）

| 變數 | 預設值 | 說明 |
|------|--------|------|
| `POSTGRES_PASSWORD` | `change_me_strong_password` | DB 密碼，**務必修改** |
| `POSTGRES_DB` | `timecard` | DB 名稱 |
| `POSTGRES_USER` | `timecard_app` | DB 使用者 |
| `POSTGRES_HOST_PORT` | `25432` | PostgreSQL 對外埠號（僅 debug 用） |
| `INITIAL_ADMIN_PASSWORD` | — | 初始 admin 帳號密碼，**必填** |
| `SYNC_PUNCH_API_KEY` | — | SyncPunch API 金鑰，**必填** |

## 本機開發

需求：.NET 10 SDK、Node.js 22+、PostgreSQL

```bash
# 後端
dotnet run --project src/Timecard.Api/Timecard.Api.csproj

# 前端（開發模式，proxy 到後端）
cd client && npm install && npm run dev
```

資料庫連線字串與 secrets：`ConnectionStrings:Timecard`（見 `appsettings.json` 或 `dotnet user-secrets`）

## API 快速測試

```bash
curl -X POST http://192.168.1.44:49178/api/clock/in   # 打卡上班
curl -X POST http://192.168.1.44:49178/api/clock/out  # 打卡下班
curl http://192.168.1.44:49178/api/day/today           # 今天摘要
curl http://192.168.1.44:49178/api/month/2026/2        # 本月報表
```
