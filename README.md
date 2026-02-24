# Timecard MVP (.NET + PostgreSQL + EF Core + Vue 3)

這是一個「個人用」的工時管理系統 MVP：
- 記錄上下班（支援多段 session）
- 手動加「調整」（請假 / 出差 / 假日 / 颱風假等）
- 日/月報表：每天 9 小時（含午休），每日彈性時數最多累積/使用 55 分鐘，每月重置

## 用 Docker 部署（推薦）

需求：Docker 與 Docker Compose

```bash
cp .env.example .env        # 複製範本並填入實際值（見下表）
docker compose up -d        # 首次啟動（自動 build image，migration 自動執行）
docker compose logs -f timecard  # 查看 log
```

App 監聽 `http://192.168.1.44:49178`。

### 環境變數（`.env`）

| 變數 | 說明 |
|------|------|
| `POSTGRES_PASSWORD` | DB 密碼，**務必修改** |
| `INITIAL_ADMIN_PASSWORD` | 初始 admin 密碼，**必填** |
| `SYNC_PUNCH_API_KEY` | SyncPunch API 金鑰，**必填** |
| `POSTGRES_HOST_PORT` | PostgreSQL 對外埠號（僅 debug 用，預設 25432） |

### 發布新版本（不動資料庫）

```bash
docker compose build timecard
docker compose up -d --no-deps timecard
```

`--no-deps` 會跳過 `postgres`，只重建 `timecard` 容器，資料完全不受影響。

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
