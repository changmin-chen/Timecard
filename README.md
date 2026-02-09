# Timecard MVP (.NET + SQLite + EF Core + Vanilla JS)

這是一個「個人用」的工時管理系統 MVP：
- 記錄上下班（支援多段 session）
- 手動加「調整」（請假 / 出差 / 假日 / 颱風假等）
- 日/月報表：每天 9 小時（含午休），每日彈性時數最多累積/使用 55 分鐘，每月重置

> 注意：這是軟體工具，不是臨床建議。

## 需求
- .NET SDK (建議 .NET 10 LTS)
- Windows / macOS / Linux 皆可

## 直接跑
```bash
cd src/Timecard.Api
dotnet restore
dotnet run
```

打開：
- http://localhost:5077/ （實際埠號以 console 顯示為準）

資料庫：
- `src/Timecard.Api/App_Data/timecard.db`

## （可選）使用 migrations（比較正統）
MVP 為了「拿到就能跑」使用 `EnsureCreated()`。
如果你想改用 migrations：

1) 安裝工具
```bash
dotnet tool install --global dotnet-ef
```

2) 把 `Program.cs` 的 `EnsureCreated()` 改成 `Migrate()`

3) 建 migration
```bash
cd src/Timecard.Api
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## API 快速測試
Clock in:
```bash
curl -X POST http://localhost:5077/api/clock/in
```

Clock out:
```bash
curl -X POST http://localhost:5077/api/clock/out
```

今天摘要:
```bash
curl http://localhost:5077/api/day/today
```

本月報表:
```bash
curl http://localhost:5077/api/month/2026/2
```
