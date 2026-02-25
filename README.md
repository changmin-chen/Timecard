# Timecard

個人工時管理系統：記錄上下班（多段 session）、手動調整出缺勤、日/月彈性工時報表。

## 部署（Docker）

需求：Docker 與 Docker Compose

```bash
cp .env.example .env        # 填入環境變數（見 docs/deployment.md）
docker compose up -d        # 首次啟動（自動 build image，migration 自動執行）
```

詳細部署設定與發布新版本流程：[docs/deployment.md](docs/deployment.md)

## 本機開發

需求：.NET 10 SDK、Node.js 22+、PostgreSQL

```bash
# 後端
dotnet run --project src/Timecard.Api/Timecard.Api.csproj

# 前端（開發模式，proxy 到後端）
cd client && npm install && npm run dev
```

資料庫連線字串：`ConnectionStrings:Timecard`（`appsettings.json` 或 `dotnet user-secrets`）

## 文件

- [部署與設定](docs/deployment.md)
- [API 參考](docs/api.md)
