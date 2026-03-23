# 部署指南

## Docker 部署

需求：Docker 與 Docker Compose

```bash
cp .env.example .env        # 複製範本並填入實際值（見下表）
docker compose up -d        # 首次啟動（自動 build image，migration 自動執行）
docker compose logs -f timecard  # 查看 log
```

監聽位址可透過 `docker-compose.yml` 的 ports 設定調整。

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

## 直接部署

```bash
cd ".\src\Timecard.Api"
dotnet publish .\Timecard.Api.csproj -c Release -o .\publish
```

### 環境變數

需要直接設定 `ConnectionStrings__Timecard`, `InitialAdmin__Password`, `SyncPunch__ApiKey`, `ASPNETCORE_ENVIRONMENT`

